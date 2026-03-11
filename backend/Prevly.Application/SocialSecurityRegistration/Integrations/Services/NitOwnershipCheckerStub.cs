using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;

namespace Prevly.Application.SocialSecurityRegistration.Integrations.Services;

public sealed class NitOwnershipCheckerStub : INitOwnershipChecker
{
    public Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default)
    {
        // Stub deterministico para manter o fluxo funcional ate integrar Playwright/MeuINSS.
        var lastDigit = nit[^1] - '0';
        var belongsToSomeone = lastDigit % 2 == 0;

        return Task.FromResult(new NitOwnershipCheckResultDto(belongsToSomeone));
    }
}
