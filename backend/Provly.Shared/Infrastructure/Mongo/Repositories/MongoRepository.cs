using MongoDB.Driver;
using Provly.Shared.Infrastructure.Mongo.Interfaces;
using Provly.Shared.Pagination;

namespace Provly.Shared.Infrastructure.Mongo.Repositories;

public class MongoRepository<T>(IMongoDatabase database, string? collectionName) : IMongoRepository<T> where T: IEntity
{
    private readonly IMongoCollection<T> _collection = database.GetCollection<T>(collectionName ?? typeof(T).Name);
    
    public async Task<T?> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<PagedResult<T>> GetPaginatedAsync(FilterDefinition<T> filter, PaginationParameters paginationParameters)
    {
        long totalRecords = await _collection.CountDocumentsAsync(filter);
        
        IList<T> data = await _collection
            .Find(filter)
            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
            .Limit(paginationParameters.PageSize)
            .ToListAsync();

        return new PagedResult<T>(
            data: data,
            pageNumber: paginationParameters.PageNumber,
            pageSize: paginationParameters.PageSize,
            totalRecords: totalRecords
        );
    }

    public async Task CreateAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    public async Task UpdateAsync(string id, T entity)
    {
        await _collection.ReplaceOneAsync(x => x.Id == id, entity);
    }

    public async Task DeleteAsync(string id)
    {
        await _collection.DeleteOneAsync(x => x.Id == id);
    }
}