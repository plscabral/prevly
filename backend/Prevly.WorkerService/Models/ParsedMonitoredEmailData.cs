using Prevly.Domain.Entities;

namespace Prevly.WorkerService.Models;

public sealed record ParsedMonitoredEmailData(
    bool IsRelevant,
    string? ExtractedName,
    string? ExtractedCpf,
    string? ExtractedBenefitNumber,
    RetirementRequestStatus? Status,
    string? Summary
);
