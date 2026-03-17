using Prevly.Api.Nit.Services;
using Prevly.Application.Nit.Dtos;
using Prevly.Application.Nit.Interfaces;

namespace Prevly.Api.Nit.Flows;

public sealed class NitDetailFlow(INitService nitService)
{
    public async Task<ContributionDetailsImportResultDto> ExecuteAsync(
        IReadOnlyCollection<PdfImportDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var processedFiles = 0;
        var updatedNits = 0;
        var notFoundNits = 0;
        var invalidFiles = 0;
        var updatedNitNumbers = new HashSet<string>();

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = new MemoryStream(document.Content, writable: false);
            var result = await nitService.ImportContributionDetailsFromPdfAsync(
                stream,
                document.FileName,
                "application/pdf",
                cancellationToken
            );

            processedFiles += result.ProcessedFiles;
            updatedNits += result.UpdatedNits;
            notFoundNits += result.NotFoundNits;
            invalidFiles += result.InvalidFiles;

            foreach (var updatedNit in result.UpdatedNitNumbers)
                updatedNitNumbers.Add(updatedNit);
        }

        return new ContributionDetailsImportResultDto(
            ProcessedFiles: processedFiles,
            UpdatedNits: updatedNits,
            NotFoundNits: notFoundNits,
            InvalidFiles: invalidFiles,
            UpdatedNitNumbers: updatedNitNumbers.ToList()
        );
    }
}
