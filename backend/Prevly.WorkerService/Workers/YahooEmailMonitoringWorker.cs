using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;

namespace Prevly.WorkerService.Workers;

public sealed class YahooEmailMonitoringWorker(
    IEmailReaderService emailReaderService,
    IProcessedEmailStore processedEmailStore,
    IEmailMessageProcessor emailMessageProcessor,
    IOptions<YahooMailMonitoringOptions> options,
    ILogger<YahooEmailMonitoringWorker> logger
) : BackgroundService
{
    private readonly YahooMailMonitoringOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("YahooEmailMonitoringWorker desabilitado por configuracao.");
            return;
        }

        var pollingInterval = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);

        logger.LogInformation(
            "YahooEmailMonitoringWorker iniciado. Server={Server}:{Port} Sender={Sender} Polling={PollingSeconds}s",
            _options.ImapServer,
            _options.ImapPort,
            _options.TargetSender,
            _options.PollingIntervalSeconds
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteCycleWithRetryAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha nao tratada no ciclo de monitoramento de e-mail.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    private async Task ExecuteCycleWithRetryAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _options.MaxRetryAttempts);
        var baseDelay = TimeSpan.FromSeconds(Math.Max(1, _options.RetryBaseDelaySeconds));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await ExecuteCycleOnceAsync(cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(baseDelay.TotalSeconds * attempt);
                logger.LogWarning(
                    ex,
                    "Falha temporaria no monitoramento de e-mail (tentativa {Attempt}/{MaxAttempts}). Novo retry em {DelaySeconds}s.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds
                );

                await Task.Delay(delay, cancellationToken);
            }
            catch
            {
                throw;
            }
        }
    }

    private async Task ExecuteCycleOnceAsync(CancellationToken cancellationToken)
    {
        var messages = await emailReaderService.GetTargetSenderMessagesAsync(cancellationToken);
        if (messages.Count == 0)
        {
            logger.LogDebug("Nenhum e-mail novo do remetente alvo encontrado neste ciclo.");
            return;
        }

        var processedNow = 0;
        var alreadyProcessed = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await processedEmailStore.IsProcessedAsync(message.UniqueKey, cancellationToken))
            {
                alreadyProcessed++;
                continue;
            }

            await emailMessageProcessor.ProcessAsync(message, cancellationToken);
            await processedEmailStore.MarkAsProcessedAsync(message.UniqueKey, cancellationToken);
            processedNow++;
        }

        logger.LogInformation(
            "Ciclo de e-mail concluido. TargetMessages={Total} ProcessedNow={ProcessedNow} AlreadyProcessed={AlreadyProcessed}",
            messages.Count,
            processedNow,
            alreadyProcessed
        );
    }

    private static bool IsTransient(Exception ex) =>
        ex is ImapProtocolException or ImapCommandException or ServiceNotConnectedException or ServiceNotAuthenticatedException;
}
