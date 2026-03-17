namespace Prevly.WorkerService.Interfaces;

public interface IProcessedEmailStore
{
    Task<bool> IsProcessedAsync(string uniqueKey, CancellationToken cancellationToken);
    Task MarkAsProcessedAsync(string uniqueKey, CancellationToken cancellationToken);
}
