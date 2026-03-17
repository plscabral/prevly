using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Prevly.Api.Nit.Flows;
using Prevly.Api.Nit.Dtos;
using Prevly.Api.Nit.Services;
using Prevly.Application.Nit.Dtos;
using Prevly.Application.Nit.Interfaces;
using Prevly.Domain.Interfaces;
using Provly.Shared.Pagination;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NitController(
    ILogger<NitController> logger,
    INitService nitService,
    IPersonRepository personRepository,
    IPdfImportFileExtractor pdfImportFileExtractor,
    NitCheckFlow nitCheckFlow,
    NitDetailFlow nitDetailFlow
) : AuthorizeController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<Prevly.Domain.Entities.Nit>>> GetPaginated(
        [FromQuery] FilterNitDto dto
    )
    {
        try
        {
            var result = await nitService.GetPaginatedAsync(dto);
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
    public async Task<ActionResult<ImportNitsResultDto>> ImportPdf(
        [FromForm] ImportPdfRequestDto request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest("Envie um arquivo .pdf, .zip ou .rar.");

            var documents = await pdfImportFileExtractor.ExtractPdfDocumentsAsync(request.File, cancellationToken);
            if (documents.Count == 0)
                return BadRequest("Nenhum PDF valido foi encontrado no arquivo enviado.");

            var result = await nitCheckFlow.ExecuteAsync(documents, request.PersonId, cancellationToken);

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
            return StatusCode(500, "Erro ao processar importacao de NITs.");
        }
    }

    [HttpPost("import-simple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportNitsResultDto>> ImportSimple(
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

            var result = await nitService.ImportFromNumbersAsync(numbers, request.PersonId);
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
            var result = await nitService.ProcessPendingVerificationsAsync(cancellationToken);
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
    public async Task<ActionResult<IReadOnlyCollection<PendingContributionNitDto>>> GetPendingPeriodExtraction()
    {
        try
        {
            var result = await nitService.GetPendingPeriodExtractionAsync();
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
                return BadRequest("Envie ao menos um arquivo .pdf, .zip ou .rar.");

            var results = new List<ContributionDetailsImportResultDto>();
            var emptyOrInvalidArchives = 0;

            foreach (var file in files)
            {
                var pdfDocuments = await pdfImportFileExtractor.ExtractPdfDocumentsAsync(file, cancellationToken);
                if (pdfDocuments.Count == 0)
                {
                    emptyOrInvalidArchives++;
                    continue;
                }

                var result = await nitDetailFlow.ExecuteAsync(pdfDocuments, cancellationToken);
                results.Add(result);
            }

            return Ok(new ContributionDetailsImportResultDto(
                ProcessedFiles: results.Sum(x => x.ProcessedFiles),
                UpdatedNits: results.Sum(x => x.UpdatedNits),
                NotFoundNits: results.Sum(x => x.NotFoundNits),
                InvalidFiles: emptyOrInvalidArchives + results.Sum(x => x.InvalidFiles),
                UpdatedNitNumbers: results
                    .SelectMany(x => x.UpdatedNitNumbers)
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
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
            await nitService.BindPersonToNitAsync(dto);
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
            var result = await nitService.CreateReportAsync(dto);
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
    public async Task<IActionResult> Export([FromBody] ExportNitsRequestDto dto)
    {
        try
        {
            var nits = await nitService.GetForExportAsync(
                dto.Query,
                dto.Status,
                dto.NitIds ?? []
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
            var personIds = nits
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
            foreach (var nit in nits)
            {
                var linkedPersonName = !string.IsNullOrWhiteSpace(nit.PersonId) &&
                                       personNameById.TryGetValue(nit.PersonId!, out var name)
                    ? name
                    : "-";

                worksheet.Cell(rowIndex, 1).Value = ValueOrDash(nit.Number);
                worksheet.Cell(rowIndex, 2).Value = nit.Status.ToString();
                worksheet.Cell(rowIndex, 3).Value = linkedPersonName;
                worksheet.Cell(rowIndex, 4).Value = ValueOrDash(nit.OwnershipOwnerName);
                worksheet.Cell(rowIndex, 5).Value = nit.FirstContributionDate?.ToString("dd/MM/yyyy") ?? "-";
                worksheet.Cell(rowIndex, 6).Value = nit.LastContributionDate?.ToString("dd/MM/yyyy") ?? "-";
                worksheet.Cell(rowIndex, 7).Value = nit.ContributionYears > 0
                    ? nit.ContributionYears
                    : "-";
                worksheet.Cell(rowIndex, 8).Value = nit.CreatedAt == default
                    ? "-"
                    : nit.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                rowIndex++;
            }

            var range = worksheet.Range(1, 1, Math.Max(1, nits.Count + 1), headers.Length);
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

            worksheet.Rows(2, Math.Max(2, nits.Count + 1)).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
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
    public sealed class ExportNitsRequestDto
    {
        public string? Query { get; init; }
        public Prevly.Domain.Entities.NitStatus? Status { get; init; }
        public List<string>? NitIds { get; init; }
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    #endregion
}
