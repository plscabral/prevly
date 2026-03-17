namespace Prevly.Api.SocialSecurityRegistration.Services;

public interface IPdfImportFileExtractor
{
    Task<IReadOnlyCollection<PdfImportDocument>> ExtractPdfDocumentsAsync(
        IFormFile file,
        CancellationToken cancellationToken = default
    );
}
