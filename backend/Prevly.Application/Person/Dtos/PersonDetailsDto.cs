namespace Prevly.Application.Services.DTOs.Person;

public sealed class PersonDetailsDto
{
    public required Prevly.Domain.Entities.Person Person { get; init; }
    public required IReadOnlyCollection<MonitoredEmailDto> MonitoredEmails { get; init; }
    public PersonRetirementAgreementDto? RetirementAgreement { get; init; }
    public IReadOnlyCollection<PersonFinancialEntryDto> FinancialEntries { get; init; } = [];
    public IReadOnlyCollection<PersonDocumentDto> Documents { get; init; } = [];
    public PersonFinancialSummaryDto? FinancialSummary { get; init; }
}
