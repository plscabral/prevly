using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using SharpCompress.Archives.Rar;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialSecurityRegistrationController(
    ILogger<SocialSecurityRegistrationController> logger,
    ISocialSecurityRegistrationService socialSecurityRegistrationService
) : AuthorizeController
{
    [HttpPost("import-pdf")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportSocialSecurityRegistrationsResultDto>> ImportPdf(
        [FromForm] ImportPdfRequestDto request
    )
    {
        try
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest("O arquivo PDF e obrigatorio.");

            await using var stream = request.File.OpenReadStream();

            var result = await socialSecurityRegistrationService.ImportFromPdfAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                request.PersonId
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

    [HttpPost("import-simple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportSocialSecurityRegistrationsResultDto>> ImportSimple(
        [FromBody] ImportSimpleRequestDto request
    )
    {
        try
        {
            var numbers = request.Numbers?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? [];

            if (!string.IsNullOrWhiteSpace(request.Number))
                numbers.Add(request.Number);

            if (numbers.Count == 0)
                return BadRequest("Informe ao menos um NIT.");

            var result = await socialSecurityRegistrationService.ImportFromNumbersAsync(numbers, request.PersonId);
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
            return StatusCode(500, "Erro ao importar NITs.");
        }
    }

    public sealed class ImportPdfRequestDto
    {
        public IFormFile? File { get; set; }
        public string? PersonId { get; set; }
    }

    public sealed class ImportSimpleRequestDto
    {
        public string? Number { get; set; }
        public List<string>? Numbers { get; set; }
        public string? PersonId { get; set; }
    }

    [HttpPost("process-pending-ownership")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessOwnershipChecksResultDto>> ProcessPendingOwnership(
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await socialSecurityRegistrationService.ProcessPendingOwnershipChecksAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao processar pendencias de consulta de titularidade.");
        }
    }

    [HttpGet("pending-contribution-calculation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<PendingContributionNitDto>>> GetPendingContributionCalculation()
    {
        try
        {
            var result = await socialSecurityRegistrationService.GetPendingContributionCalculationAsync();
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao buscar NITs pendentes de calculo de contribuicao.");
        }
    }

    [HttpPost("import-contribution-details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContributionDetailsImportResultDto>> ImportContributionDetails(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (files.Count == 0)
                return BadRequest("Envie ao menos um PDF de detalhe de NIT.");

            var processedFiles = 0;
            var updatedRegistrations = 0;
            var notFoundNits = 0;
            var invalidFiles = 0;
            var updatedNitNumbers = new HashSet<string>();

            foreach (var file in files)
            {
                var pdfDocuments = await ExtractPdfDocumentsAsync(file, cancellationToken);
                if (pdfDocuments.Count == 0)
                {
                    invalidFiles++;
                    continue;
                }

                foreach (var document in pdfDocuments)
                {
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
            }

            return Ok(new ContributionDetailsImportResultDto(
                ProcessedFiles: processedFiles,
                UpdatedRegistrations: updatedRegistrations,
                NotFoundNits: notFoundNits,
                InvalidFiles: invalidFiles,
                UpdatedNitNumbers: updatedNitNumbers.ToList()
            ));
        }
        catch (ArgumentException e)
        {
            logger.LogWarning(e, e.Message);
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao importar detalhes de contribuicao.");
        }
    }

    [HttpPost("bind-person")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BindPersonToNit([FromBody] BindPersonToNitDto dto)
    {
        try
        {
            await socialSecurityRegistrationService.BindPersonToNitAsync(dto);
            return NoContent();
        }
        catch (ArgumentException e)
        {
            logger.LogWarning(e, e.Message);
            return BadRequest(e.Message);
        }
        catch (InvalidOperationException e)
        {
            logger.LogWarning(e, e.Message);
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao vincular person ao NIT.");
        }
    }

    [HttpPost("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<NitReportItemDto>>> CreateReport(
        [FromBody] CreateNitReportRequestDto dto
    )
    {
        try
        {
            var result = await socialSecurityRegistrationService.CreateReportAsync(dto);
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao gerar relatorio de NITs.");
        }
    }

    #region Private methods

    private static async Task<IReadOnlyCollection<ContributionPdfDocument>> ExtractPdfDocumentsAsync(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => [await ReadPdfAsync(file, cancellationToken)],
            ".zip" => await ReadZipAsync(file, cancellationToken),
            ".rar" => await ReadRarAsync(file, cancellationToken),
            _ => []
        };
    }

    private static async Task<ContributionPdfDocument> ReadPdfAsync(
        IFormFile file, 
        CancellationToken cancellationToken
    )
    {
        await using var inputStream = file.OpenReadStream();
        await using var outputStream = new MemoryStream();
        await inputStream.CopyToAsync(outputStream, cancellationToken);

        return new ContributionPdfDocument(file.FileName, outputStream.ToArray());
    }

    private static async Task<IReadOnlyCollection<ContributionPdfDocument>> ReadZipAsync(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        var documents = new List<ContributionPdfDocument>();
        await using var inputStream = file.OpenReadStream();
        using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                continue;

            if (!Path.GetExtension(entry.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var entryStream = entry.Open();
            await using var outputStream = new MemoryStream();
            await entryStream.CopyToAsync(outputStream, cancellationToken);
            documents.Add(new ContributionPdfDocument(entry.Name, outputStream.ToArray()));
        }

        return documents;
    }

    private static async Task<IReadOnlyCollection<ContributionPdfDocument>> ReadRarAsync(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        var documents = new List<ContributionPdfDocument>();
        await using var inputStream = file.OpenReadStream();
        using var archive = RarArchive.Open(inputStream);

        foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
        {
            var entryKey = entry.Key;
            if (string.IsNullOrWhiteSpace(entryKey))
                continue;

            if (!Path.GetExtension(entryKey).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var entryStream = entry.OpenEntryStream();
            await using var outputStream = new MemoryStream();
            await entryStream.CopyToAsync(outputStream, cancellationToken);
            var fileName = Path.GetFileName(entryKey);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "document.pdf";

            documents.Add(new ContributionPdfDocument(fileName, outputStream.ToArray()));
        }

        return documents;
    }

    private sealed record ContributionPdfDocument(string FileName, byte[] Content);

    #endregion
}
