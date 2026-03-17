using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Prevly.Api.SocialSecurityRegistration.Flows;
using Prevly.Api.SocialSecurityRegistration.Services;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Person.Services;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Integrations.Services;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Services;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Prevly.Infrastructure;
using Provly.Shared.Security;
using Provly.Shared.Settings;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(nameof(MongoDbSettings)));
builder.Services.AddSingleton<MongoDbSettings>(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

// mongodb
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<MongoDbSettings>();
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<MongoDbSettings>();
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

//jwt
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
        ["http://localhost:3000"];
    
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// services
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<ISocialSecurityRegistrationService, SocialSecurityRegistrationService>();
builder.Services.AddScoped<IPdfImportFileExtractor, PdfImportFileExtractor>();
builder.Services.AddScoped<NitCheckFlow>();
builder.Services.AddScoped<NitDetailFlow>();


// repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<ISocialSecurityRegistrationRepository, SocialSecurityRegistrationRepository>();

// builder.Services.AddScoped<NitOwnershipChecker>();
builder.Services.AddScoped<INitOwnershipChecker, NitOwnershipChecker>();

var app = builder.Build();

await SeedDefaultAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

static async Task SeedDefaultAdminAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedDefaultAdmin");
    var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

    try
    {
        var filter = Builders<Account>.Filter.Eq(x => x.Login, "admin");
        var existing = await accountRepository.GetOneAsync(filter);

        if (existing is not null)
            return;

        await accountRepository.CreateAsync(new Account
        {
            Name = "Administrador",
            Login = "admin",
            Password = "admin",
            CreatedAt = DateTime.UtcNow
        });

        logger.LogInformation("Conta padrao admin/admin criada com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Nao foi possivel garantir a conta padrao admin/admin.");
    }
}
