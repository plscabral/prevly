using Prevly.Application.Nit.Dtos;

namespace Prevly.Application.Nit.Integrations.Interfaces;

public interface INitOwnershipChecker
{
    Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default);
}
