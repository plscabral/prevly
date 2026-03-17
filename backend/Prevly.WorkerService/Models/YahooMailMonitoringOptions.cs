using System.ComponentModel.DataAnnotations;

namespace Prevly.WorkerService.Models;

public sealed class YahooMailMonitoringOptions
{
    public const string SectionName = "YahooMailMonitoring";

    public bool Enabled { get; set; } = true;

    [Required]
    public string ImapServer { get; set; } = "imap.mail.yahoo.com";

    [Range(1, 65535)]
    public int ImapPort { get; set; } = 993;

    public bool UseSsl { get; set; } = true;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Range(5, 3600)]
    public int PollingIntervalSeconds { get; set; } = 30;

    [Required]
    public string TargetSender { get; set; } = "noreply@inss.gov.br";

    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(1, 60)]
    public int RetryBaseDelaySeconds { get; set; } = 5;

    [Required]
    public string ProcessedStoreFilePath { get; set; } = "data/processed-emails.json";
}
