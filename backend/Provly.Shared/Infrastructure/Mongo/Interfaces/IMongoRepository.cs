using MongoDB.Driver;
using Provly.Shared.Pagination;

namespace Provly.Shared.Infrastructure.Mongo.Interfaces;

public interface IMongoRepository<T> where T : IEntity
{
    Task<T?> GetByIdAsync(string id);
    Task<PagedResult<T>> GetPaginatedAsync(FilterDefinition<T> filter, PaginationParameters paginationParameters);
    Task CreateAsync(T entity);
    Task UpdateAsync(string id, T entity);
    Task DeleteAsync(string id);
}