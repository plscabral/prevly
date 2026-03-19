using Prevly.Domain.Entities;

namespace Prevly.Application.Person.Services;

public static class RetirementRequestStatusLabelMapper
{
    public static string ToPtBrLabel(RetirementRequestStatus? status) => status switch
    {
        RetirementRequestStatus.PendingRequirement => "Aguardando exigência(s)",
        RetirementRequestStatus.Approved => "Deferido",
        RetirementRequestStatus.Denied => "Benefício negado",
        RetirementRequestStatus.UnderAnalysis => "Em análise",
        _ => "Sem status"
    };
}
