using Prevly.Application.Services.DTOs;
using Prevly.Application.Services.DTOs.Person;
using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Application.Services;

public interface IPersonService
{
    Task<PagedResult<Person>> GetPaginatedAsync(FilterPersonDto dto);
    Task<Person?> GetByIdAsync(string id);
    Task<Person> CreateAsync(CreatePersonDto dto);
    Task<Person?> UpdateAsync(string id, UpdatePersonDto dto);
    Task<bool> DeleteAsync(string id);
}
