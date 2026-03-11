namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record BindPersonToNitDto(
    string SocialSecurityRegistrationId,
    string PersonId
);
