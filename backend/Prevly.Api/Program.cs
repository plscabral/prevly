using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Prevly.Api.Documents;
using Prevly.Api.Nit.Flows;
using Prevly.Api.Nit.Services;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Person.Services;
using Prevly.Application.Nit.Integrations.Interfaces;
using Prevly.Application.Nit.Integrations.Services;
using Prevly.Application.Nit.Interfaces;
using Prevly.Application.Nit.Services;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Prevly.Infrastructure;
using Provly.Shared.Security;
using Provly.Shared.Settings;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(nameof(MongoDbSettings)));
builder.Services.AddSingleton<MongoDbSettings>(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);
builder.Services
    .AddOptions<DocumentStorageOptions>()
    .Bind(builder.Configuration.GetSection(DocumentStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<DocumentStorageOptions>(sp =>
    sp.GetRequiredService<IOptions<DocumentStorageOptions>>().Value
);

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
builder.Services.AddScoped<INitService, NitService>();
builder.Services.AddScoped<IPdfImportFileExtractor, PdfImportFileExtractor>();
builder.Services.AddScoped<NitCheckFlow>();
builder.Services.AddScoped<NitDetailFlow>();
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var settings = sp.GetRequiredService<DocumentStorageOptions>();
    var config = new AmazonS3Config
    {
        ServiceURL = settings.Endpoint,
        ForcePathStyle = settings.ForcePathStyle,
        AuthenticationRegion = RegionEndpoint.USEast1.SystemName
    };

    var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
    return new AmazonS3Client(credentials, config);
});
builder.Services.AddScoped<IDocumentStorage, R2DocumentStorage>();


// repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IMonitoredEmailRepository, MonitoredEmailRepository>();
builder.Services.AddScoped<INitRepository, NitRepository>();

// builder.Services.AddScoped<NitOwnershipChecker>();
builder.Services.AddScoped<INitOwnershipChecker, NitOwnershipChecker>();

var app = builder.Build();

await SeedDefaultAdminAsync(app.Services);
await NormalizeLegacyRetirementStatusDataAsync(app.Services);

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

static async Task NormalizeLegacyRetirementStatusDataAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NormalizeLegacyRetirementStatusData");
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

    try
    {
        var personsCollection = database.GetCollection<BsonDocument>(nameof(Person));
        var monitoredEmailsCollection = database.GetCollection<BsonDocument>(nameof(MonitoredEmail));

        var statusMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Aguardando cumprimento de exigência"] = nameof(RetirementRequestStatus.PendingRequirement),
            ["Aguardando cumprimento de exigencia"] = nameof(RetirementRequestStatus.PendingRequirement),
            ["Aguardando exigência(s)"] = nameof(RetirementRequestStatus.PendingRequirement),
            ["Aguardando exigencia(s)"] = nameof(RetirementRequestStatus.PendingRequirement),
            ["Indeferido"] = nameof(RetirementRequestStatus.Denied),
            ["Benefício negado"] = nameof(RetirementRequestStatus.Denied),
            ["Beneficio negado"] = nameof(RetirementRequestStatus.Denied),
            ["Deferido"] = nameof(RetirementRequestStatus.Approved),
            ["Benefício aprovado"] = nameof(RetirementRequestStatus.Approved),
            ["Beneficio aprovado"] = nameof(RetirementRequestStatus.Approved),
            ["Em análise"] = nameof(RetirementRequestStatus.UnderAnalysis),
            ["Em analise"] = nameof(RetirementRequestStatus.UnderAnalysis)
        };

        var normalizedPersons = 0L;
        var normalizedMonitoredEmails = 0L;

        foreach (var (legacyValue, enumValue) in statusMappings)
        {
            var personFilter = Builders<BsonDocument>.Filter.Eq("RetirementRequestStatus", legacyValue);
            var personUpdate = Builders<BsonDocument>.Update.Set("RetirementRequestStatus", enumValue);
            var personResult = await personsCollection.UpdateManyAsync(personFilter, personUpdate);
            normalizedPersons += personResult.ModifiedCount;

            var monitoredEmailFilter = Builders<BsonDocument>.Filter.Eq("IdentifiedStatus", legacyValue);
            var monitoredEmailUpdate = Builders<BsonDocument>.Update.Set("IdentifiedStatus", enumValue);
            var monitoredEmailResult = await monitoredEmailsCollection.UpdateManyAsync(monitoredEmailFilter, monitoredEmailUpdate);
            normalizedMonitoredEmails += monitoredEmailResult.ModifiedCount;
        }

        var copyDateFilter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Exists("RetirementRequestStatusUpdatedAt", true),
            Builders<BsonDocument>.Filter.Ne("RetirementRequestStatusUpdatedAt", BsonNull.Value),
            Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Exists("RetirementRequestStatusLastEmailUpdatedAt", false),
                Builders<BsonDocument>.Filter.Eq("RetirementRequestStatusLastEmailUpdatedAt", BsonNull.Value)
            )
        );
        var documentsToBackfill = await personsCollection
            .Find(copyDateFilter)
            .Project(Builders<BsonDocument>.Projection
                .Include("_id")
                .Include("RetirementRequestStatusUpdatedAt"))
            .ToListAsync();

        var copiedDatesCount = 0L;
        foreach (var document in documentsToBackfill)
        {
            if (!document.TryGetValue("_id", out var idValue) ||
                !document.TryGetValue("RetirementRequestStatusUpdatedAt", out var dateValue))
            {
                continue;
            }

            var updateFilter = Builders<BsonDocument>.Filter.Eq("_id", idValue);
            var update = Builders<BsonDocument>.Update.Set("RetirementRequestStatusLastEmailUpdatedAt", dateValue);
            var result = await personsCollection.UpdateOneAsync(updateFilter, update);
            copiedDatesCount += result.ModifiedCount;
        }

        if (normalizedPersons > 0 || normalizedMonitoredEmails > 0 || copiedDatesCount > 0)
        {
            logger.LogInformation(
                "Normalizacao de status aplicada. PersonsStatus={PersonsStatus} MonitoredEmailsStatus={MonitoredEmailsStatus} PersonsLastEmailDate={PersonsLastEmailDate}",
                normalizedPersons,
                normalizedMonitoredEmails,
                copiedDatesCount
            );
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Nao foi possivel normalizar dados legados de status.");
    }
}
