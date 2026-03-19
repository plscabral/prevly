using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Prevly.WorkerService.Interfaces;

namespace Prevly.WorkerService.Services;

public sealed class PersonRetirementStatusService(
    IPersonRepository personRepository,
    ILogger<PersonRetirementStatusService> logger
) : IPersonRetirementStatusService
{
    public async Task UpdateStatusAsync(
        Person person,
        RetirementRequestStatus status,
        DateTimeOffset messageReceivedAt,
        string? benefitNumber,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var shouldUpdate = person.RetirementRequestStatusUpdatedAt is null ||
                           messageReceivedAt.UtcDateTime >= person.RetirementRequestStatusUpdatedAt.Value;

        if (!shouldUpdate)
            return;

        person.RetirementRequestStatus = status;
        person.RetirementRequestStatusUpdatedAt = messageReceivedAt.UtcDateTime;

        if (status == RetirementRequestStatus.Approved && !string.IsNullOrWhiteSpace(benefitNumber))
        {
            person.RetirementBenefitNumber = new string(benefitNumber.Where(char.IsDigit).ToArray());
        }

        await personRepository.UpdateAsync(person.Id!, person);

        logger.LogInformation(
            "Status previdenciario atualizado. PersonId={PersonId} Status={Status} BenefitNumber={BenefitNumber} MessageReceivedAt={MessageReceivedAt:O}",
            person.Id,
            status,
            person.RetirementBenefitNumber ?? "(n/d)",
            messageReceivedAt
        );
    }
}
