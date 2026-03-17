using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Services;

public sealed class LoggingEmailMessageProcessor(
    ILogger<LoggingEmailMessageProcessor> logger
) : IEmailMessageProcessor
{
    public Task ProcessAsync(EmailMessageInfo message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Email alvo recebido. MessageId={MessageId} Subject={Subject} From={From} ReceivedAt={ReceivedAt:O}",
            message.MessageId,
            message.Subject,
            message.From,
            message.ReceivedAt
        );

        return Task.CompletedTask;
    }
}
