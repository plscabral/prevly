namespace Prevly.Application.Services.DTOs.Person;

public sealed class PersonDetailsDto
{
    public required Prevly.Domain.Entities.Person Person { get; init; }
    public required IReadOnlyCollection<MonitoredEmailDto> MonitoredEmails { get; init; }
}
