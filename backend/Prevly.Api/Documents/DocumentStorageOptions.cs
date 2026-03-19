using System.ComponentModel.DataAnnotations;

namespace Prevly.Api.Documents;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string BucketName { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    public bool ForcePathStyle { get; init; } = true;
    public int PresignedUrlExpirationMinutes { get; init; } = 10;
}
