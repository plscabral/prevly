using Prevly.Application.SocialSecurityRegistration.Dtos;

namespace Prevly.Application.SocialSecurityRegistration.Interfaces;

public interface ISocialSecurityRegistrationService
{
    Task<ImportSocialSecurityRegistrationsResultDto> ImportFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? personId
    );
    Task<ProcessOwnershipChecksResultDto> ProcessPendingOwnershipChecksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PendingContributionNitDto>> GetPendingContributionCalculationAsync();
    Task<ContributionDetailsImportResultDto> ImportContributionDetailsFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    );
    Task BindPersonToNitAsync(BindPersonToNitDto dto);
    Task<IReadOnlyCollection<NitReportItemDto>> CreateReportAsync(CreateNitReportRequestDto dto);
}
