using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Integrations.Services;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Services;
using Prevly.Domain.Interfaces;
using Prevly.Infrastructure;
using Provly.Shared.Settings;
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
builder.Services.AddScoped<ISocialSecurityRegistrationRepository, SocialSecurityRegistrationRepository>();
builder.Services.AddScoped<ISocialSecurityRegistrationService, SocialSecurityRegistrationService>();

builder.Services.AddScoped<NitOwnershipChecker>();
builder.Services.AddScoped<INitOwnershipChecker>(sp => sp.GetRequiredService<NitOwnershipChecker>());
builder.Services.AddHostedService<NitOwnershipCheckWorker>();

var host = builder.Build();
host.Run();
