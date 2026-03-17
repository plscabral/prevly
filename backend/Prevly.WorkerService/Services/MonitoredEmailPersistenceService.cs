using System.Security.Cryptography;
using System.Text;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Services;

public sealed class MonitoredEmailPersistenceService(
    IMonitoredEmailRepository monitoredEmailRepository
) : IMonitoredEmailPersistenceService
{
    public async Task<bool> IsAlreadyProcessedAsync(EmailMessageInfo message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messageUniqueId = BuildMessageUniqueId(message);
        if (!string.IsNullOrWhiteSpace(messageUniqueId) &&
            await monitoredEmailRepository.ExistsByMessageUniqueIdAsync(messageUniqueId))
        {
            return true;
        }

        var hash = ComputeContentHash(message);
        return await monitoredEmailRepository.ExistsByContentHashAsync(hash);
    }

    public async Task PersistAsync(
        Person person,
        EmailMessageInfo message,
        ParsedMonitoredEmailData parsedData,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = new MonitoredEmail
        {
            PersonId = person.Id!,
            Subject = message.Subject,
            From = message.From,
            RawContent = message.RawContent,
            Summary = parsedData.Summary,
            ReceivedAt = message.ReceivedAt,
            IdentifiedStatus = parsedData.Status,
            ExtractedName = parsedData.ExtractedName,
            ExtractedCpf = parsedData.ExtractedCpf,
            MessageUniqueId = BuildMessageUniqueId(message),
            ContentHash = ComputeContentHash(message),
            CreatedAt = DateTime.UtcNow
        };

        await monitoredEmailRepository.CreateAsync(entity);
    }

    private static string? BuildMessageUniqueId(EmailMessageInfo message)
    {
        if (!string.IsNullOrWhiteSpace(message.MessageId))
            return message.MessageId.Trim();

        return string.IsNullOrWhiteSpace(message.UniqueKey)
            ? null
            : message.UniqueKey.Trim();
    }

    private static string ComputeContentHash(EmailMessageInfo message)
    {
        var raw = $"{message.Subject}|{message.From}|{message.ReceivedAt:O}|{message.RawContent}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
