namespace Prevly.Application.Nit.Dtos;

public sealed record ImportNitsResultDto(
    int TotalCandidates,
    int TotalValidNits,
    int Inserted,
    int Duplicates,
    IReadOnlyCollection<string> Numbers
);
