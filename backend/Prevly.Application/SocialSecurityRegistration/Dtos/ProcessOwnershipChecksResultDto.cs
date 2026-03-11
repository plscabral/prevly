namespace Prevly.Application.SocialSecurityRegistration.Dtos;

public sealed record ProcessOwnershipChecksResultDto(
    int Processed,
    int MovedToContributionCalculation,
    int RejectedOwnedByAnotherPerson,
    int Errors
);
