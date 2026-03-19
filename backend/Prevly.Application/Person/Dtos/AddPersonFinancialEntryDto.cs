using Prevly.Domain.Entities;

namespace Prevly.Application.Services.DTOs.Person;

public sealed class AddPersonFinancialEntryDto
{
    public PersonFinancialEntryType Type { get; init; } = PersonFinancialEntryType.Other;
    public string? Description { get; init; }
    public decimal Value { get; init; }
    public DateTime Date { get; init; } = DateTime.UtcNow;
    public string? Origin { get; init; }
    public string? Notes { get; init; }
}

