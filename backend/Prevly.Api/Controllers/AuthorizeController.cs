using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Provly.Shared.Security;
using System.Security.Claims;

namespace Prevly.Api.Controllers;

[Authorize]
public class AuthorizeController : ControllerBase
{
    protected AuthenticatedAccount GetSessionInfo()
    {
        return new AuthenticatedAccount(
            AccountId: User.FindFirstValue(PrevlyClaimTypes.AccountId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
            AccountName: User.FindFirstValue(PrevlyClaimTypes.AccountName) ?? User.FindFirstValue(ClaimTypes.Name),
            AccountEmail: User.FindFirstValue(PrevlyClaimTypes.AccountEmail) ?? User.FindFirstValue(ClaimTypes.Email)
        );
    }
}

public sealed record AuthenticatedAccount(string? AccountId, string? AccountName, string? AccountEmail);
