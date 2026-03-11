using Provly.Shared.Pagination;

namespace Prevly.Application.Services.DTOs.Person;

public class FilterPersonDto : PaginationParameters
{
    public string? Name { get; init; }
    public string? Cpf { get; init; }
}