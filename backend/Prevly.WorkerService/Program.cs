using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prevly.Application.Nit.Integrations.Interfaces;
using Prevly.Application.Nit.Integrations.Services;
using Prevly.Application.Nit.Interfaces;
using Prevly.Application.Nit.Services;
using Prevly.Domain.Interfaces;
using Prevly.Infrastructure;
using Provly.Shared.Settings;
using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;
using Prevly.WorkerService.Services;
using Prevly.WorkerService.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(nameof(MongoDbSettings)));
builder.Services.AddSingleton<MongoDbSettings>(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

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

// repositories
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IMonitoredEmailRepository, MonitoredEmailRepository>();
builder.Services.AddScoped<INitRepository, NitRepository>();
builder.Services.AddScoped<INitService, NitService>();

builder.Services.AddScoped<NitOwnershipChecker>();
builder.Services.AddScoped<INitOwnershipChecker>(sp => sp.GetRequiredService<NitOwnershipChecker>());

builder.Services
    .AddOptions<YahooMailMonitoringOptions>()
    .Bind(builder.Configuration.GetSection(YahooMailMonitoringOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Password) && !string.IsNullOrWhiteSpace(options.Username),
        "YahooMailMonitoring username/password sao obrigatorios."
    )
    .ValidateOnStart();

builder.Services.AddSingleton<IEmailReaderService, YahooImapEmailReaderService>();
builder.Services.AddScoped<IEmailContentParserService, EmailContentParserService>();
builder.Services.AddScoped<IPersonResolverService, PersonResolverService>();
builder.Services.AddScoped<IMonitoredEmailPersistenceService, MonitoredEmailPersistenceService>();
builder.Services.AddScoped<IPersonRetirementStatusService, PersonRetirementStatusService>();
builder.Services.AddScoped<IEmailMessageProcessor, LoggingEmailMessageProcessor>();

// workers ----------------------------------------------------------------------------------------------------------------
var workerName = args.Length > 0 ? args[0] : "YahooEmailMonitoringWorker";
switch (workerName)
{
    case "NitOwnershipCheckWorker":
        builder.Services.AddHostedService<NitOwnershipCheckWorker>();
        break;    
    
    case "YahooEmailMonitoringWorker":
        builder.Services.AddHostedService<YahooEmailMonitoringWorker>();
        break;

    default:
        builder.Services.AddHostedService<YahooEmailMonitoringWorker>();
        break;
}

var host = builder.Build();
host.Run();
