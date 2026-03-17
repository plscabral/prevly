namespace Prevly.Application.Nit.Dtos;

public sealed record PendingContributionNitDto(
    string Id,
    string Number,
    DateTime CreatedAt
);
