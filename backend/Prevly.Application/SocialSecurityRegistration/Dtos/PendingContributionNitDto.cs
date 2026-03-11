namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record PendingContributionNitDto(
    string Id,
    string Number,
    DateTime CreatedAt
);
