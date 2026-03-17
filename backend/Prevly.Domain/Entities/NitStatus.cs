namespace Prevly.Domain.Entities;

public enum NitStatus
{
    PendingVerification,
    VerificationInProgress,
    NotFound,
    Unbound,
    Bound,
    PendingPeriodExtraction,
    PeriodExtractionInProgress,
    ReadyToUse,
    QueryError
}
