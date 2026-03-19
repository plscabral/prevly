using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Services;

public sealed class LoggingEmailMessageProcessor(
    IEmailContentParserService parserService,
    IPersonResolverService personResolverService,
    IMonitoredEmailPersistenceService monitoredEmailPersistenceService,
    IPersonRetirementStatusService personRetirementStatusService,
    ILogger<LoggingEmailMessageProcessor> logger
) : IEmailMessageProcessor
{
    public async Task ProcessAsync(EmailMessageInfo message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await monitoredEmailPersistenceService.IsAlreadyProcessedAsync(message, cancellationToken))
        {
            logger.LogDebug("E-mail ignorado por deduplicacao. MessageId={MessageId}", message.MessageId);
            return;
        }

        var parsedData = parserService.Parse(message);
        if (!parsedData.IsRelevant)
        {
            logger.LogDebug("E-mail ignorado por nao ser relevante. MessageId={MessageId}", message.MessageId);
            return;
        }

        if (string.IsNullOrWhiteSpace(parsedData.ExtractedName))
        {
            logger.LogWarning(
                "E-mail relevante sem nome para vinculo de pessoa. MessageId={MessageId} Name={Name} Cpf={Cpf}",
                message.MessageId,
                parsedData.ExtractedName ?? "(n/d)",
                parsedData.ExtractedCpf ?? "(n/d)"
            );
            return;
        }

        var person = await personResolverService.ResolveAsync(
            parsedData.ExtractedName,
            parsedData.ExtractedCpf,
            cancellationToken
        );

        await monitoredEmailPersistenceService.PersistAsync(person, message, parsedData, cancellationToken);

        if (parsedData.Status is not null)
        {
            await personRetirementStatusService.UpdateStatusAsync(
                person,
                parsedData.Status.Value,
                message.ReceivedAt,
                parsedData.ExtractedBenefitNumber,
                cancellationToken
            );
        }

        logger.LogInformation(
            "Email monitorado processado. MessageId={MessageId} Subject={Subject} From={From} ReceivedAt={ReceivedAt:O}",
            message.MessageId,
            message.Subject,
            message.From,
            message.ReceivedAt
        );
    }
}
