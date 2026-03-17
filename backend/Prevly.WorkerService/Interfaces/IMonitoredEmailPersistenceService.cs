using Prevly.Domain.Entities;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Interfaces;

public interface IMonitoredEmailPersistenceService
{
    Task<bool> IsAlreadyProcessedAsync(EmailMessageInfo message, CancellationToken cancellationToken);
    Task PersistAsync(Person person, EmailMessageInfo message, ParsedMonitoredEmailData parsedData, CancellationToken cancellationToken);
}
