using Prevly.Application.Services.DTOs.Person;
using Provly.Shared.Pagination;

namespace Prevly.Application.Person.Interfaces;

public interface IPersonService
{
    Task<PagedResult<Domain.Entities.Person>> GetPaginatedAsync(FilterPersonDto dto);
    Task<Domain.Entities.Person?> GetByIdAsync(string id);
    Task<Domain.Entities.Person> CreateAsync(CreatePersonDto dto);
    Task<Domain.Entities.Person?> UpdateAsync(string id, UpdatePersonDto dto);
    Task<bool> DeleteAsync(string id);
}
