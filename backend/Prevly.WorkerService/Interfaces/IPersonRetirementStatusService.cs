using Prevly.Domain.Entities;

namespace Prevly.WorkerService.Interfaces;

public interface IPersonRetirementStatusService
{
    Task UpdateStatusAsync(
        Person person,
        RetirementRequestStatus status,
        DateTimeOffset messageReceivedAt,
        string? benefitNumber,
        CancellationToken cancellationToken
    );
}
