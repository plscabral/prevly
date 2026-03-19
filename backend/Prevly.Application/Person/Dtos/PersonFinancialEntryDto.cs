using Prevly.Domain.Entities;

namespace Prevly.Application.Services.DTOs.Person;

public sealed class PersonFinancialEntryDto
{
    public string? Id { get; init; }
    public PersonFinancialEntryType Type { get; init; }
    public string? Description { get; init; }
    public decimal Value { get; init; }
    public DateTime Date { get; init; }
    public string? Origin { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

