using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;
using System.Text;

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

        var lookbackDate = DateTime.UtcNow.AddDays(-Math.Max(1, _options.LookbackDays));
        var searchQuery = SearchQuery.NotDeleted
            .And(SearchQuery.DeliveredAfter(lookbackDate))
            .And(SearchQuery.FromContains(_options.TargetSender));

        var allUids = await inbox.SearchAsync(searchQuery, cancellationToken);
        if (allUids.Count == 0)
        {
            await client.DisconnectAsync(true, cancellationToken);
            return [];
        }

        var selectedUids = allUids
            .OrderByDescending(x => x.Id)
            .Take(Math.Max(1, _options.MaxMessagesPerCycle))
            .OrderBy(x => x.Id)
            .ToList();

        var summaries = await inbox.FetchAsync(
            selectedUids,
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

            var mimeMessage = await inbox.GetMessageAsync(summary.UniqueId, cancellationToken);
            var rawContent = BuildRawContent(mimeMessage.TextBody, mimeMessage.HtmlBody);

            result.Add(new EmailMessageInfo(
                UniqueKey: uniqueKey,
                MessageId: messageId,
                Subject: subject,
                From: fromAddress.Trim(),
                ReceivedAt: receivedAt,
                RawContent: rawContent,
                Summary: Summarize(rawContent)
            ));
        }

        await client.DisconnectAsync(true, cancellationToken);

        logger.LogDebug(
            "Leitura IMAP concluida. MatchedMessages={MatchedMessages} LoadedMessages={LoadedMessages}",
            allUids.Count,
            result.Count
        );

        return result
            .OrderBy(x => x.ReceivedAt)
            .ToList();
    }

    private static string BuildRawContent(string? textBody, string? htmlBody)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(textBody))
            sb.AppendLine(textBody);

        if (!string.IsNullOrWhiteSpace(htmlBody))
            sb.AppendLine(htmlBody);

        return sb.ToString().Trim();
    }

    private static string? Summarize(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return null;

        var compact = string.Join(
            " ",
            rawContent
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );

        if (compact.Length <= 280)
            return compact;

        return compact[..280] + "...";
    }
}
