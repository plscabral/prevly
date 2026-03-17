using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Interfaces;

public interface IEmailReaderService
{
    Task<IReadOnlyCollection<EmailMessageInfo>> GetTargetSenderMessagesAsync(CancellationToken cancellationToken);
}
