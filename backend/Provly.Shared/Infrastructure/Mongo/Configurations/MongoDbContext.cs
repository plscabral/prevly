
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Provly.Shared.Settings;

namespace Provly.Shared.Infrastructure.Mongo.Configurations;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoDatabase GetDatabase() => _database;
}