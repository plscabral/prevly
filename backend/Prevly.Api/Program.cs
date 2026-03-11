using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Person.Services;
using Prevly.Application.Services;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Integrations.Settings;
using Prevly.Application.SocialSecurityRegistration.Integrations.Services;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Services;
using Prevly.Api.Workers;
using Prevly.Domain.Interfaces;
using Prevly.Infrastructure;
using Provly.Shared.Security;
using Provly.Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

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

// repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<ISocialSecurityRegistrationRepository, SocialSecurityRegistrationRepository>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<ISocialSecurityRegistrationService, SocialSecurityRegistrationService>();
builder.Services.Configure<NitOwnershipCheckerSettings>(builder.Configuration.GetSection("NitOwnershipChecker"));
builder.Services.AddScoped<NitOwnershipCheckerStub>();
builder.Services.AddScoped<NitOwnershipCheckerPlaywright>();
builder.Services.AddScoped<INitOwnershipChecker>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<NitOwnershipCheckerSettings>>().Value;

    return settings.Provider.Equals("Stub", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<NitOwnershipCheckerStub>()
        : sp.GetRequiredService<NitOwnershipCheckerPlaywright>();
});
builder.Services.AddHostedService<NitOwnershipCheckWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapHealthChecks("/healthz");


app.Run();
