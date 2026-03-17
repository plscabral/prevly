namespace Prevly.WorkerService.Models;

public sealed record EmailMessageInfo(
    string UniqueKey,
    string MessageId,
    string Subject,
    string From,
    DateTimeOffset ReceivedAt,
    string RawContent,
    string? Summary
);
