using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Services;

public sealed class YahooImapEmailReaderService(
    IOptions<YahooMailMonitoringOptions> options,
    ILogger<YahooImapEmailReaderService> logger
) : IEmailReaderService
{
    private readonly YahooMailMonitoringOptions _options = options.Value;

    public async Task<IReadOnlyCollection<EmailMessageInfo>> GetTargetSenderMessagesAsync(CancellationToken cancellationToken)
    {
        using var client = new ImapClient();

        await client.ConnectAsync(
            _options.ImapServer,
            _options.ImapPort,
            _options.UseSsl,
            cancellationToken
        );

        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        // Busca completa + filtro exato do remetente para garantir igualdade com TargetSender.
        var allUids = await inbox.SearchAsync(SearchQuery.NotDeleted, cancellationToken);
        if (allUids.Count == 0)
        {
            await client.DisconnectAsync(true, cancellationToken);
            return [];
        }

        var summaries = await inbox.FetchAsync(
            allUids,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate,
            cancellationToken
        );

        var targetSender = _options.TargetSender.Trim();
        var result = new List<EmailMessageInfo>();

        foreach (var summary in summaries)
        {
            var fromAddress = summary.Envelope?.From?.Mailboxes.FirstOrDefault()?.Address;
            if (string.IsNullOrWhiteSpace(fromAddress))
                continue;

            if (!string.Equals(fromAddress.Trim(), targetSender, StringComparison.OrdinalIgnoreCase))
                continue;

            var uniqueKey = $"{inbox.UidValidity}:{summary.UniqueId.Id}";
            var messageId = summary.Envelope?.MessageId?.Trim();
            if (string.IsNullOrWhiteSpace(messageId))
                messageId = uniqueKey;

            var receivedAt = summary.InternalDate ?? DateTimeOffset.UtcNow;
            var subject = string.IsNullOrWhiteSpace(summary.Envelope?.Subject)
                ? "(sem assunto)"
                : summary.Envelope.Subject.Trim();

            result.Add(new EmailMessageInfo(
                UniqueKey: uniqueKey,
                MessageId: messageId,
                Subject: subject,
                From: fromAddress.Trim(),
                ReceivedAt: receivedAt
            ));
        }

        await client.DisconnectAsync(true, cancellationToken);

        logger.LogDebug(
            "Leitura IMAP concluida. TotalMessages={TotalMessages} TargetSenderMessages={TargetSenderMessages}",
            summaries.Count,
            result.Count
        );

        return result
            .OrderBy(x => x.ReceivedAt)
            .ToList();
    }
}
