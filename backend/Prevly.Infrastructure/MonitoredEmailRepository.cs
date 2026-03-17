using MongoDB.Driver;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Infrastructure.Mongo.Repositories;

namespace Prevly.Infrastructure;

public sealed class MonitoredEmailRepository : MongoRepository<MonitoredEmail>, IMonitoredEmailRepository
{
    private readonly IMongoCollection<MonitoredEmail> _collection;

    public MonitoredEmailRepository(IMongoDatabase database)
        : base(database, nameof(MonitoredEmail))
    {
        _collection = database.GetCollection<MonitoredEmail>(nameof(MonitoredEmail));
        EnsureIndexes();
    }

    public Task<bool> ExistsByMessageUniqueIdAsync(string messageUniqueId)
    {
        if (string.IsNullOrWhiteSpace(messageUniqueId))
            return Task.FromResult(false);

        var filter = Builders<MonitoredEmail>.Filter.Eq(x => x.MessageUniqueId, messageUniqueId.Trim());
        return _collection.Find(filter).AnyAsync();
    }

    public Task<bool> ExistsByContentHashAsync(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            return Task.FromResult(false);

        var filter = Builders<MonitoredEmail>.Filter.Eq(x => x.ContentHash, contentHash.Trim());
        return _collection.Find(filter).AnyAsync();
    }

    public async Task<IReadOnlyCollection<MonitoredEmail>> GetByPersonIdAsync(string personId)
    {
        if (string.IsNullOrWhiteSpace(personId))
            return [];

        var filter = Builders<MonitoredEmail>.Filter.Eq(x => x.PersonId, personId.Trim());
        return await _collection
            .Find(filter)
            .SortByDescending(x => x.ReceivedAt)
            .ToListAsync();
    }

    private void EnsureIndexes()
    {
        var personIdIndex = new CreateIndexModel<MonitoredEmail>(
            Builders<MonitoredEmail>.IndexKeys.Ascending(x => x.PersonId),
            new CreateIndexOptions { Name = "idx_monitored_email_person_id" }
        );

        var messageUniqueIdIndex = new CreateIndexModel<MonitoredEmail>(
            Builders<MonitoredEmail>.IndexKeys.Ascending(x => x.MessageUniqueId),
            new CreateIndexOptions
            {
                Name = "uk_monitored_email_message_unique_id",
                Unique = true,
                Sparse = true
            }
        );

        var contentHashIndex = new CreateIndexModel<MonitoredEmail>(
            Builders<MonitoredEmail>.IndexKeys.Ascending(x => x.ContentHash),
            new CreateIndexOptions
            {
                Name = "uk_monitored_email_content_hash",
                Unique = true,
                Sparse = true
            }
        );

        _collection.Indexes.CreateMany([personIdIndex, messageUniqueIdIndex, contentHashIndex]);
    }
}
