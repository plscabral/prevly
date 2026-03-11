using Prevly.Application.SocialSecurityRegistration.Dtos;

namespace Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;

public interface INitOwnershipChecker
{
    Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default);
}
