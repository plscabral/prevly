namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record ImportSocialSecurityRegistrationsResultDto(
    int TotalCandidates,
    int TotalValidNits,
    int Inserted,
    int Duplicates,
    IReadOnlyCollection<string> Numbers
);
