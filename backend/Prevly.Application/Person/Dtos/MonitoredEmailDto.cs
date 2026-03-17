namespace Prevly.Application.Services.DTOs.Person;

public sealed class MonitoredEmailDto
{
    public string? Id { get; init; }
    public string PersonId { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string? From { get; init; }
    public string? RawContent { get; init; }
    public string? Summary { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public Prevly.Domain.Entities.RetirementRequestStatus? IdentifiedStatus { get; init; }
    public string? IdentifiedStatusLabel { get; init; }
    public string? ExtractedName { get; init; }
    public string? ExtractedCpf { get; init; }
    public string? MessageUniqueId { get; init; }
    public DateTime CreatedAt { get; init; }
}
