using Prevly.Domain.Entities;

namespace Prevly.WorkerService.Models;

public sealed record ParsedMonitoredEmailData(
    bool IsRelevant,
    string? ExtractedName,
    string? ExtractedCpf,
    RetirementRequestStatus? Status,
    string? Summary
);
