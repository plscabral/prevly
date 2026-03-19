namespace Prevly.Application.Services.DTOs.Person;

public sealed class PersonFinancialSummaryDto
{
    public decimal? OperationalCostTotal { get; init; }
    public decimal? TotalPaid { get; init; }
    public decimal? TotalOpen { get; init; }
    public decimal? OutstandingBalance { get; init; }
    public decimal? EstimatedSalaryCountToSettle { get; init; }
    public decimal? EstimatedInstallmentsToSettle { get; init; }
    public decimal? ClientNetMonthlyValue { get; init; }
}

