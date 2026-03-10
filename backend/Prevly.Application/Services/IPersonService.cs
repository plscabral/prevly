using Prevly.Application.Services.Models;
using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Application.Services;

public interface IPersonService
{
    Task<PagedResult<Person>> GetPaginatedAsync(PersonPaginationParameters parameters);
    Task<Person?> GetByIdAsync(string id);
    Task<Person> CreateAsync(CreatePersonRequest request);
    Task<Person?> UpdateAsync(string id, UpdatePersonRequest request);
    Task<bool> DeleteAsync(string id);
}
