namespace Prevly.Api.Documents;

public interface IDocumentStorage
{
    Task UploadAsync(Stream content, string key, string? contentType, CancellationToken cancellationToken);
    Task<DocumentFile?> DownloadAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    string GetPrivateViewUrl(string key, string? fileName, string? contentType);
    string GetPrivateDownloadUrl(string key, string? fileName, string? contentType);
    string GetPrivateUploadUrl(string key, string? contentType);
}

public sealed record DocumentFile(byte[] Content, string? ContentType);
