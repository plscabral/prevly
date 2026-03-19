using Amazon.S3;
using Amazon.S3.Model;

namespace Prevly.Api.Documents;

public sealed class R2DocumentStorage(
    IAmazonS3 s3Client,
    DocumentStorageOptions options
) : IDocumentStorage
{
    public async Task UploadAsync(Stream content, string key, string? contentType, CancellationToken cancellationToken)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            UseChunkEncoding = false,
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(putRequest, cancellationToken);
    }

    public async Task<DocumentFile?> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await s3Client.GetObjectAsync(options.BucketName, key, cancellationToken);
            await using var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            return new DocumentFile(memory.ToArray(), response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await s3Client.DeleteObjectAsync(options.BucketName, key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(options.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public string GetPrivateViewUrl(string key, string? fileName, string? contentType)
        => BuildPrivateUrl(key, fileName, contentType, "inline");

    public string GetPrivateDownloadUrl(string key, string? fileName, string? contentType)
        => BuildPrivateUrl(key, fileName, contentType, "attachment");

    public string GetPrivateUploadUrl(string key, string? contentType)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(options.PresignedUrlExpirationMinutes)
        };

        if (!string.IsNullOrWhiteSpace(contentType))
            request.ContentType = contentType;

        return s3Client.GetPreSignedURL(request);
    }

    private string BuildPrivateUrl(
        string key,
        string? fileName,
        string? contentType,
        string dispositionType
    )
    {
        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? "documento" : fileName.Trim();
        var escapedFileName = Uri.EscapeDataString(safeFileName);
        var responseHeaderOverrides = new ResponseHeaderOverrides
        {
            ContentDisposition = $"{dispositionType}; filename*=UTF-8''{escapedFileName}"
        };

        if (!string.IsNullOrWhiteSpace(contentType))
            responseHeaderOverrides.ContentType = contentType;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(options.PresignedUrlExpirationMinutes),
            ResponseHeaderOverrides = responseHeaderOverrides
        };

        return s3Client.GetPreSignedURL(request);
    }
}
