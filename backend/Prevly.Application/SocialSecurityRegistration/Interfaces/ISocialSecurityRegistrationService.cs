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
}
