using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Security;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAccountRepository accountRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("authenticate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthenticateResponse>> Authenticate([FromBody] AuthenticateRequest request)
    {
        var email = request.Email.Trim();
        var account = await accountRepository.GetOneAsync(
            Builders<Account>.Filter.Eq(x => x.Email, email));

        if (account is null || !string.Equals(account.Password, request.Password, StringComparison.Ordinal))
            return Unauthorized(new { message = "Email ou senha invalidos." });

        if (string.IsNullOrWhiteSpace(account.Id) ||
            string.IsNullOrWhiteSpace(account.Name) ||
            string.IsNullOrWhiteSpace(account.Email))
        {
            return Problem(
                detail: "A conta encontrada nao possui os dados necessarios para autenticacao.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var token = jwtTokenGenerator.GenerateToken(account.Id, account.Name, account.Email);

        return Ok(new AuthenticateResponse(
            Token: token,
            ExpiresInMinutes: GetExpirationInMinutes(),
            AccountId: account.Id,
            Name: account.Name,
            Email: account.Email
        ));
    }

    private int GetExpirationInMinutes()
    {
        var expirationValue = configuration.GetSection("JwtSettings")["ExpirationInMinutes"];

        return int.TryParse(expirationValue, out var expirationInMinutes)
            ? expirationInMinutes
            : 0;
    }
}

public sealed class AuthenticateRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthenticateResponse(
    string Token,
    int ExpiresInMinutes,
    string AccountId,
    string Name,
    string Email);
