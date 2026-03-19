namespace Prevly.Domain.Entities;

public class PersonRetirementAgreement
{
    public decimal? TotalCost { get; set; }
    public PersonOperationalCostType OperationalCostType { get; set; } = PersonOperationalCostType.Simple;
    public decimal? OperationalCostSimpleValue { get; set; }
    public List<PersonOperationalCostItem> OperationalCostItems { get; set; } = [];
    public decimal? MonthlyRetirementValue { get; set; }

    public PersonPaymentType PaymentType { get; set; } = PersonPaymentType.Custom;
    public bool HasDownPayment { get; set; }
    public decimal? DownPaymentValue { get; set; }
    public DateTime? DownPaymentDate { get; set; }
    public bool DiscountFromBenefit { get; set; }
    public decimal? MonthlyAmountForSettlement { get; set; }
    public string? FinancialNotes { get; set; }
}

