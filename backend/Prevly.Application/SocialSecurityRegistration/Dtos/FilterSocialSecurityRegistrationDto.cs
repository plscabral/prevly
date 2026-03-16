using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed class FilterSocialSecurityRegistrationDto : PaginationParameters
{
    public string? Number { get; init; }
    public SocialSecurityRegistrationStatus? Status { get; init; }
    public string? PersonId { get; init; }
}
