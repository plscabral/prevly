using Prevly.Domain.Entities;

namespace Prevly.Application.Person.Services;

public static class RetirementRequestStatusLabelMapper
{
    public static string ToPtBrLabel(RetirementRequestStatus? status) => status switch
    {
        RetirementRequestStatus.PendingRequirement => "Aguardando cumprimento de exigência",
        RetirementRequestStatus.Approved => "Deferido",
        RetirementRequestStatus.Denied => "Indeferido",
        _ => "Sem status"
    };
}
