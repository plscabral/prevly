using System.Text.Json;
using Microsoft.Extensions.Options;
using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Persistence;

public sealed class FileProcessedEmailStore(
    IOptions<YahooMailMonitoringOptions> options,
    ILogger<FileProcessedEmailStore> logger
) : IProcessedEmailStore
{
    private readonly YahooMailMonitoringOptions _options = options.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private HashSet<string>? _processedIds;

    public async Task<bool> IsProcessedAsync(string uniqueKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uniqueKey))
            return false;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _processedIds!.Contains(uniqueKey);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task MarkAsProcessedAsync(string uniqueKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uniqueKey))
            return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            if (_processedIds!.Add(uniqueKey))
            {
                await PersistAsync(cancellationToken);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_processedIds is not null)
            return;

        var filePath = ResolveFilePath();
        if (!File.Exists(filePath))
        {
            _processedIds = new HashSet<string>(StringComparer.Ordinal);
            await PersistAsync(cancellationToken);
            return;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var document = await JsonSerializer.DeserializeAsync<ProcessedEmailStoreDocument>(
                stream,
                _jsonOptions,
                cancellationToken
            );

            _processedIds = document?.ProcessedIds is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(document.ProcessedIds, StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao carregar arquivo de controle. Um novo arquivo sera criado.");
            _processedIds = new HashSet<string>(StringComparer.Ordinal);
            await PersistAsync(cancellationToken);
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var filePath = ResolveFilePath();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(filePath);
        var document = new ProcessedEmailStoreDocument(_processedIds!.ToList());
        await JsonSerializer.SerializeAsync(stream, document, _jsonOptions, cancellationToken);
    }

    private string ResolveFilePath()
    {
        var configuredPath = _options.ProcessedStoreFilePath.Trim();
        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    private sealed record ProcessedEmailStoreDocument(IReadOnlyCollection<string> ProcessedIds);
}
