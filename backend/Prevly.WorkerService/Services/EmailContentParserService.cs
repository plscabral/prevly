using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Prevly.Domain.Entities;
using Prevly.WorkerService.Interfaces;
using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Services;

public sealed class EmailContentParserService : IEmailContentParserService
{
    private static readonly Regex CpfRegex = new(@"(?<!\d)\d{3}\.?\d{3}\.?\d{3}-?\d{2}(?!\d)", RegexOptions.Compiled);
    private static readonly Regex[] NamePatterns =
    [
        new Regex(@"\bNome(?:\s+do\s+segurado)?\s*:\s*(?<name>[A-Za-zÀ-ÿ\s]{5,120})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"\bSegurado(?:\(a\))?\s*:\s*(?<name>[A-Za-zÀ-ÿ\s]{5,120})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"\bInteressado(?:\(a\))?\s*:\s*(?<name>[A-Za-zÀ-ÿ\s]{5,120})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"\bPrezado\(a\)\s*Sr\(a\)\s*(?<name>[A-Za-zÀ-ÿ\s]{5,120})\s*,", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    ];

    public ParsedMonitoredEmailData Parse(EmailMessageInfo message)
    {
        var searchable = BuildSearchableText(message);
        var normalizedSearchable = Normalize(searchable);

        var status = TryExtractStatus(normalizedSearchable);
        var extractedCpf = TryExtractCpf(searchable);
        var extractedName = TryExtractName(searchable);

        var isRelevant = status is not null
            || normalizedSearchable.Contains("aposentadoria", StringComparison.Ordinal)
            || normalizedSearchable.Contains("status do requerimento", StringComparison.Ordinal)
            || normalizedSearchable.Contains("requerimento inss", StringComparison.Ordinal);

        return new ParsedMonitoredEmailData(
            IsRelevant: isRelevant,
            ExtractedName: extractedName,
            ExtractedCpf: extractedCpf,
            Status: status,
            Summary: message.Summary
        );
    }

    private static string BuildSearchableText(EmailMessageInfo message)
    {
        var builder = new StringBuilder();
        builder.AppendLine(message.Subject);
        builder.AppendLine(message.RawContent);
        return builder.ToString();
    }

    private static string? TryExtractCpf(string content)
    {
        var match = CpfRegex.Match(content);
        if (!match.Success)
            return null;

        var digits = new string(match.Value.Where(char.IsDigit).ToArray());
        return digits.Length == 11 ? digits : null;
    }

    private static string? TryExtractName(string content)
    {
        foreach (var pattern in NamePatterns)
        {
            var match = pattern.Match(content);
            if (!match.Success)
                continue;

            var candidate = match.Groups["name"].Value.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            return CollapseSpaces(candidate);
        }

        return null;
    }

    private static RetirementRequestStatus? TryExtractStatus(string normalizedSearchable)
    {
        if (normalizedSearchable.Contains("aguardando cumprimento de exigencia", StringComparison.Ordinal) ||
            normalizedSearchable.Contains("cumprimento de exigencia", StringComparison.Ordinal) ||
            normalizedSearchable.Contains("status atual : exigencia", StringComparison.Ordinal) ||
            normalizedSearchable.Contains("alterado para exigencia", StringComparison.Ordinal))
        {
            return RetirementRequestStatus.PendingRequirement;
        }

        if (normalizedSearchable.Contains("indeferido", StringComparison.Ordinal) ||
            normalizedSearchable.Contains("pedido indeferido", StringComparison.Ordinal))
        {
            return RetirementRequestStatus.Denied;
        }

        if (normalizedSearchable.Contains("deferido", StringComparison.Ordinal) ||
            normalizedSearchable.Contains("pedido deferido", StringComparison.Ordinal))
        {
            return RetirementRequestStatus.Approved;
        }

        return null;
    }

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private static string CollapseSpaces(string value) =>
        string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
