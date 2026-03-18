using MongoDB.Driver;
using Provly.Shared.Pagination;

namespace Provly.Shared.Infrastructure.Mongo.Interfaces;

public interface IMongoRepository<T> where T : IEntity
{
    Task<T?> GetByIdAsync(string id);
    Task<T?> GetOneAsync(FilterDefinition<T> filter);
    Task<PagedResult<T>> GetPaginatedAsync(
        FilterDefinition<T> filter,
        PaginationParameters paginationParameters,
        SortDefinition<T>? sort = null
    );
    Task CreateAsync(T entity);
    Task UpdateAsync(string id, T entity);
    Task DeleteAsync(string id);
}
