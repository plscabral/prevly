namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record NitOwnershipCheckResultDto(
    bool BelongsToSomeone,
    string? OwnerName = null
);
