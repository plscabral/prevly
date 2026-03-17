using Prevly.Api.SocialSecurityRegistration.Services;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Interfaces;

namespace Prevly.Api.SocialSecurityRegistration.Flows;

public sealed class NitDetailFlow(ISocialSecurityRegistrationService socialSecurityRegistrationService)
{
    public async Task<ContributionDetailsImportResultDto> ExecuteAsync(
        IReadOnlyCollection<PdfImportDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var processedFiles = 0;
        var updatedRegistrations = 0;
        var notFoundNits = 0;
        var invalidFiles = 0;
        var updatedNitNumbers = new HashSet<string>();

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = new MemoryStream(document.Content, writable: false);
            var result = await socialSecurityRegistrationService.ImportContributionDetailsFromPdfAsync(
                stream,
                document.FileName,
                "application/pdf",
                cancellationToken
            );

            processedFiles += result.ProcessedFiles;
            updatedRegistrations += result.UpdatedRegistrations;
            notFoundNits += result.NotFoundNits;
            invalidFiles += result.InvalidFiles;

            foreach (var updatedNit in result.UpdatedNitNumbers)
                updatedNitNumbers.Add(updatedNit);
        }

        return new ContributionDetailsImportResultDto(
            ProcessedFiles: processedFiles,
            UpdatedRegistrations: updatedRegistrations,
            NotFoundNits: notFoundNits,
            InvalidFiles: invalidFiles,
            UpdatedNitNumbers: updatedNitNumbers.ToList()
        );
    }
}
