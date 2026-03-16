namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record NitReportItemDto(
    string NitNumber,
    string? PersonId,
    string? PersonName,
    string? PersonCpf,
    int ContributionYears
);
