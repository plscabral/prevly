namespace Prevly.Domain.Entities;

public enum SocialSecurityRegistrationStatus
{
    PendingOwnershipCheck = 0,
    OwnershipCheckInProgress = 1,
    RejectedOwnedByAnotherPerson = 2,
    PendingContributionCalculation = 3,
    ReadyForPersonBinding = 4,
    BoundToPerson = 5
}
