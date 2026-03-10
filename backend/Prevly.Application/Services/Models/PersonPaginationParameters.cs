using Provly.Shared.Pagination;

namespace Prevly.Application.Services.Models;

public sealed class PersonPaginationParameters : PaginationParameters
{
    public string? Name { get; init; }
    public string? Cpf { get; init; }
}
