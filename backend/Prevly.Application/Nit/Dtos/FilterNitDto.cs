using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Application.Nit.Dtos;

public sealed class FilterNitDto : PaginationParameters
{
    public string? Number { get; init; }
    public NitStatus? Status { get; init; }
    public string? PersonId { get; init; }
}
