namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record CreateNitReportRequestDto(IReadOnlyCollection<string> RegistrationIds);
