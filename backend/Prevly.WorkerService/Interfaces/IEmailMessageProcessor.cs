using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Interfaces;

public interface IEmailMessageProcessor
{
    Task ProcessAsync(EmailMessageInfo message, CancellationToken cancellationToken);
}
