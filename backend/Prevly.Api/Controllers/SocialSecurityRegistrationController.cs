using System.IO.Compression;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Prevly.Api.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using Prevly.Domain.Interfaces;
using Provly.Shared.Pagination;
using SharpCompress.Archives.Rar;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialSecurityRegistrationController(
    ILogger<SocialSecurityRegistrationController> logger,
    ISocialSecurityRegistrationService socialSecurityRegistrationService,
    IPersonRepository personRepository
) : AuthorizeController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<Prevly.Domain.Entities.SocialSecurityRegistration>>> GetPaginated(
        [FromQuery] FilterSocialSecurityRegistrationDto dto
    )
    {
        try
        {
            var result = await socialSecurityRegistrationService.GetPaginatedAsync(dto);
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

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

    [HttpPost("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Export([FromBody] ExportSocialSecurityRegistrationsRequestDto dto)
    {
        try
        {
            var registrations = await socialSecurityRegistrationService.GetForExportAsync(
                dto.Query,
                dto.Status,
                dto.RegistrationIds ?? []
            );

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("NITs");

            var headers = new[]
            {
                "Número NIT",
                "Status",
                "Pessoa",
                "Titular",
                "Data início",
                "Data fim",
                "Anos",
                "Criado em"
            };

            for (var col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            var personNameById = new Dictionary<string, string>(StringComparer.Ordinal);
            var personIds = registrations
                .Select(x => x.PersonId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var personId in personIds)
            {
                var person = await personRepository.GetByIdAsync(personId!);
                if (!string.IsNullOrWhiteSpace(person?.Name))
                {
                    personNameById[personId!] = person.Name!;
                }
            }

            var rowIndex = 2;
            foreach (var registration in registrations)
            {
                var linkedPersonName = !string.IsNullOrWhiteSpace(registration.PersonId) &&
                                       personNameById.TryGetValue(registration.PersonId!, out var name)
                    ? name
                    : "-";

                worksheet.Cell(rowIndex, 1).Value = ValueOrDash(registration.Number);
                worksheet.Cell(rowIndex, 2).Value = registration.Status.ToString();
                worksheet.Cell(rowIndex, 3).Value = linkedPersonName;
                worksheet.Cell(rowIndex, 4).Value = ValueOrDash(registration.OwnershipOwnerName);
                worksheet.Cell(rowIndex, 5).Value = registration.FirstContributionDate?.ToString("dd/MM/yyyy") ?? "-";
                worksheet.Cell(rowIndex, 6).Value = registration.LastContributionDate?.ToString("dd/MM/yyyy") ?? "-";
                worksheet.Cell(rowIndex, 7).Value = registration.ContributionYears > 0
                    ? registration.ContributionYears
                    : "-";
                worksheet.Cell(rowIndex, 8).Value = registration.CreatedAt == default
                    ? "-"
                    : registration.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                rowIndex++;
            }

            var range = worksheet.Range(1, 1, Math.Max(1, registrations.Count + 1), headers.Length);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#334155");
            range.Style.Border.InsideBorderColor = XLColor.FromHtml("#334155");

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Rows(2, Math.Max(2, registrations.Count + 1)).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 18);
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 16);
            worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 16);
            worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 24);
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 14);
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 14);
            worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, 10);
            worksheet.Column(8).Width = Math.Max(worksheet.Column(8).Width, 18);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"prevly-nits-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao exportar relatorio de NITs.");
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

    public sealed class ExportSocialSecurityRegistrationsRequestDto
    {
        public string? Query { get; init; }
        public Prevly.Domain.Entities.SocialSecurityRegistrationStatus? Status { get; init; }
        public List<string>? RegistrationIds { get; init; }
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    #endregion
}
