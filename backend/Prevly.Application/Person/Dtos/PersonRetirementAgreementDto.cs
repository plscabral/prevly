using Prevly.Domain.Entities;

namespace Prevly.Application.Services.DTOs.Person;

public sealed class PersonRetirementAgreementDto
{
    public decimal? TotalCost { get; init; }
    public PersonOperationalCostType OperationalCostType { get; init; }
    public decimal? OperationalCostSimpleValue { get; init; }
    public IReadOnlyCollection<PersonOperationalCostItemDto> OperationalCostItems { get; init; } = [];
    public decimal? MonthlyRetirementValue { get; init; }
    public PersonPaymentType PaymentType { get; init; }
    public bool HasDownPayment { get; init; }
    public decimal? DownPaymentValue { get; init; }
    public DateTime? DownPaymentDate { get; init; }
    public bool DiscountFromBenefit { get; init; }
    public decimal? MonthlyAmountForSettlement { get; init; }
    public string? FinancialNotes { get; init; }
}

