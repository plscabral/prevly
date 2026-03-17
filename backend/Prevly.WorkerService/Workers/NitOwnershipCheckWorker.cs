using Prevly.Application.Nit.Interfaces;

namespace Prevly.WorkerService.Workers;

public sealed class NitOwnershipCheckWorker(
    IServiceProvider serviceProvider,
    ILogger<NitOwnershipCheckWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INitService>();

                var result = await service.ProcessPendingOwnershipChecksAsync(stoppingToken);
                if (result.Processed > 0 || result.Errors > 0)
                {
                    logger.LogInformation(
                        "NitOwnershipCheckWorker processed={Processed} movedToContribution={Moved} rejected={Rejected} errors={Errors}",
                        result.Processed,
                        result.MovedToContributionCalculation,
                        result.RejectedOwnedByAnotherPerson,
                        result.Errors
                    );
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "NitOwnershipCheckWorker execution failed.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }
}
