using System.IO.Compression;
using SharpCompress.Archives.Rar;

namespace Prevly.Api.Nit.Services;

public sealed class PdfImportFileExtractor : IPdfImportFileExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".zip",
        ".rar"
    };

    public async Task<IReadOnlyCollection<PdfImportDocument>> ExtractPdfDocumentsAsync(
        IFormFile file,
        CancellationToken cancellationToken = default
    )
    {
        var extension = Path.GetExtension(file.FileName);
        if (!SupportedExtensions.Contains(extension))
            throw new ArgumentException("Formato invalido. Envie um arquivo .pdf, .zip ou .rar.");

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => [await ReadPdfAsync(file, cancellationToken)],
            ".zip" => await ReadZipAsync(file, cancellationToken),
            ".rar" => await ReadRarAsync(file, cancellationToken),
            _ => []
        };
    }

    private static async Task<PdfImportDocument> ReadPdfAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var inputStream = file.OpenReadStream();
        await using var outputStream = new MemoryStream();
        await inputStream.CopyToAsync(outputStream, cancellationToken);

        return new PdfImportDocument(file.FileName, outputStream.ToArray());
    }

    private static async Task<IReadOnlyCollection<PdfImportDocument>> ReadZipAsync(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        var documents = new List<PdfImportDocument>();
        await using var inputStream = file.OpenReadStream();
        using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                continue;

            if (!Path.GetExtension(entry.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var entryStream = entry.Open();
            await using var outputStream = new MemoryStream();
            await entryStream.CopyToAsync(outputStream, cancellationToken);
            documents.Add(new PdfImportDocument(entry.Name, outputStream.ToArray()));
        }

        return documents;
    }

    private static async Task<IReadOnlyCollection<PdfImportDocument>> ReadRarAsync(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        var documents = new List<PdfImportDocument>();
        await using var inputStream = file.OpenReadStream();
        using var archive = RarArchive.Open(inputStream);

        foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
        {
            var entryKey = entry.Key;
            if (string.IsNullOrWhiteSpace(entryKey))
                continue;

            if (!Path.GetExtension(entryKey).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var entryStream = entry.OpenEntryStream();
            await using var outputStream = new MemoryStream();
            await entryStream.CopyToAsync(outputStream, cancellationToken);
            var fileName = Path.GetFileName(entryKey);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "document.pdf";

            documents.Add(new PdfImportDocument(fileName, outputStream.ToArray()));
        }

        return documents;
    }
}
