namespace Prevly.Application.Nit.Dtos;

public sealed record ProcessOwnershipChecksResultDto(
    int Processed,
    int MovedToPendingPeriodExtraction,
    int NotFound,
    int Errors
);
