namespace Prevly.Domain.Entities;

public class PersonOperationalCostItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

