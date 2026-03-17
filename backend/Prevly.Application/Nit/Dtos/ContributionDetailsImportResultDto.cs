namespace Prevly.Application.Nit.Dtos;

public sealed record ContributionDetailsImportResultDto(
    int ProcessedFiles,
    int UpdatedNits,
    int NotFoundNits,
    int InvalidFiles,
    IReadOnlyCollection<string> UpdatedNitNumbers
);
