namespace Prevly.Application.Nit.Dtos;

public sealed record NitOwnershipCheckResultDto(
    bool BelongsToSomeone,
    string? OwnerName = null
);
