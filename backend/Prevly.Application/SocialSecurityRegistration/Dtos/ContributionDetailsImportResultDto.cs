namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record ContributionDetailsImportResultDto(
    int ProcessedFiles,
    int UpdatedRegistrations,
    int NotFoundNits,
    int InvalidFiles,
    IReadOnlyCollection<string> UpdatedNitNumbers
);
