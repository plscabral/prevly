namespace Prevly.Domain.Entities;

public class PersonFinancialEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public PersonFinancialEntryType Type { get; set; } = PersonFinancialEntryType.Other;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Origin { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

