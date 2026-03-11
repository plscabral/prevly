using Microsoft.AspNetCore.Mvc;
using Prevly.Application.Services;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Interfaces;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialSecurityRegistrationController(
    ILogger<SocialSecurityRegistrationController> logger,
    ISocialSecurityRegistrationService socialSecurityRegistrationService
    ) : AuthorizeController
{
    [HttpPost("import-pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportSocialSecurityRegistrationsResultDto>> ImportPdf(
        [FromForm] IFormFile? file,
        [FromForm] string? personId = null
    )
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest("O arquivo PDF e obrigatorio.");

            await using var stream = file.OpenReadStream();
            
            var result = await socialSecurityRegistrationService.ImportFromPdfAsync(
                stream,
                file.FileName,
                file.ContentType,
                personId
            );
            
            return Ok(result);
        }
        catch (ArgumentException e)
        {
            logger.LogWarning(e, e.Message);
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao importar o PDF.");
        }
    }
}
