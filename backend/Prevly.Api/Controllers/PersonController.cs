using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using Prevly.Api.Documents;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Person.Services;
using Prevly.Application.Services.DTOs.Person;
using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(
    ILogger<PersonController> logger,
    IPersonService personService,
    IDocumentStorage documentStorage
    ) : AuthorizeController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<Person>>> GetPaginated([FromQuery] FilterPersonDto dto)
    {
        try
        {
            var result = await personService.GetPaginatedAsync(dto);
            
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Person>> GetById([FromRoute] string id)
    {
        try
        {
            var person = await personService.GetByIdAsync(id);

            return person is null ? NotFound() : Ok(person);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpGet("{id}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonDetailsDto>> GetDetails([FromRoute] string id)
    {
        try
        {
            var details = await personService.GetDetailsAsync(id);
            return details is null ? NotFound() : Ok(details);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Person>> Create([FromBody] CreatePersonDto dto)
    {
        try
        {
            var person = await personService.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Person>> Update([FromRoute] string id, [FromBody] UpdatePersonDto dto)
    {
        try
        {
            var person = await personService.UpdateAsync(id, dto);

            return person is null ? NotFound() : Ok(person);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPut("{id}/retirement-agreement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Person>> UpdateRetirementAgreement(
        [FromRoute] string id,
        [FromBody] UpsertPersonRetirementAgreementDto dto
    )
    {
        try
        {
            var person = await personService.UpdateRetirementAgreementAsync(id, dto);
            return person is null ? NotFound() : Ok(person);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPost("{id}/financial-entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonFinancialEntry>> AddFinancialEntry(
        [FromRoute] string id,
        [FromBody] AddPersonFinancialEntryDto dto
    )
    {
        try
        {
            var entry = await personService.AddFinancialEntryAsync(id, dto);
            return entry is null ? NotFound() : Ok(entry);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpDelete("{id}/financial-entries/{entryId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFinancialEntry([FromRoute] string id, [FromRoute] string entryId)
    {
        try
        {
            var deleted = await personService.DeleteFinancialEntryAsync(id, entryId);
            return deleted ? NoContent() : NotFound();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPost("{id}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonDocument>> UploadDocument(
        [FromRoute] string id,
        [FromForm] UploadPersonDocumentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest("Arquivo obrigatório.");

            var extension = Path.GetExtension(request.File.FileName);
            var safeFileName = Path.GetFileName(request.File.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                safeFileName = $"documento{extension}";

            var storageFileName = $"{Guid.NewGuid():N}{extension}";
            var storageKey = $"person-documents/{id}/{storageFileName}";
            await using var uploadStream = request.File.OpenReadStream();
            await documentStorage.UploadAsync(
                uploadStream,
                storageKey,
                request.File.ContentType,
                cancellationToken
            );

            var document = await personService.AddDocumentAsync(id, new AddPersonDocumentDto
            {
                DocumentType = request.DocumentType,
                FileName = safeFileName,
                StorageKey = storageKey,
                ContentType = request.File.ContentType,
                Description = request.Description,
                CreatedBy = request.CreatedBy,
                UploadedAt = DateTime.UtcNow
            });

            if (document is null)
            {
                await documentStorage.DeleteAsync(storageKey, cancellationToken);
                return NotFound();
            }

            return Ok(document);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPost("{id}/documents/upload-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<CreateDocumentUploadUrlResponseDto> CreateDocumentUploadUrl(
        [FromRoute] string id,
        [FromBody] CreateDocumentUploadUrlRequestDto request
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("Nome do arquivo é obrigatório.");

            var safeFileName = Path.GetFileName(request.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                return BadRequest("Nome do arquivo inválido.");

            var extension = Path.GetExtension(safeFileName);
            var storageFileName = $"{Guid.NewGuid():N}{extension}";
            var storageKey = $"person-documents/{id}/{storageFileName}";
            var contentType = string.IsNullOrWhiteSpace(request.ContentType)
                ? "application/octet-stream"
                : request.ContentType;

            var uploadUrl = documentStorage.GetPrivateUploadUrl(storageKey, contentType);
            return Ok(new CreateDocumentUploadUrlResponseDto
            {
                StorageKey = storageKey,
                UploadUrl = uploadUrl
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao gerar URL de upload.");
        }
    }

    [HttpPost("{id}/documents/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonDocument>> CompleteDocumentUpload(
        [FromRoute] string id,
        [FromBody] CompleteDocumentUploadRequestDto request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.StorageKey))
                return BadRequest("StorageKey obrigatório.");

            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("Nome do arquivo obrigatório.");

            var exists = await documentStorage.ExistsAsync(request.StorageKey, cancellationToken);
            if (!exists)
                return BadRequest("Arquivo não encontrado no storage para conclusão do upload.");

            var document = await personService.AddDocumentAsync(id, new AddPersonDocumentDto
            {
                DocumentType = request.DocumentType,
                FileName = Path.GetFileName(request.FileName),
                StorageKey = request.StorageKey.Trim(),
                ContentType = request.ContentType,
                Description = request.Description,
                CreatedBy = request.CreatedBy,
                UploadedAt = DateTime.UtcNow
            });

            return document is null ? NotFound() : Ok(document);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpGet("{id}/documents/{documentId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadDocument(
        [FromRoute] string id,
        [FromRoute] string documentId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var document = await personService.GetDocumentAsync(id, documentId);
            if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
                return NotFound();

            var file = await documentStorage.DownloadAsync(document.StorageKey, cancellationToken);
            if (file is null)
                return NotFound();

            return File(
                file.Content,
                document.ContentType ?? file.ContentType ?? "application/octet-stream",
                document.FileName
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao baixar documento.");
        }
    }

    [HttpGet("{id}/documents/{documentId}/view-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPrivateViewUrl(
        [FromRoute] string id,
        [FromRoute] string documentId
    )
    {
        try
        {
            var document = await personService.GetDocumentAsync(id, documentId);
            if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
                return NotFound();

            var url = documentStorage.GetPrivateViewUrl(
                document.StorageKey,
                document.FileName,
                document.ContentType
            );

            return Ok(new PrivateDocumentViewUrlResponseDto { Url = url });
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao gerar URL privada do documento.");
        }
    }

    [HttpGet("{id}/documents/{documentId}/download-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPrivateDownloadUrl(
        [FromRoute] string id,
        [FromRoute] string documentId
    )
    {
        try
        {
            var document = await personService.GetDocumentAsync(id, documentId);
            if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
                return NotFound();

            var url = documentStorage.GetPrivateDownloadUrl(
                document.StorageKey,
                document.FileName,
                document.ContentType
            );

            return Ok(new PrivateDocumentViewUrlResponseDto { Url = url });
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao gerar URL privada de download.");
        }
    }

    [HttpDelete("{id}/documents/{documentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocument(
        [FromRoute] string id,
        [FromRoute] string documentId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var document = await personService.GetDocumentAsync(id, documentId);
            if (document is null)
                return NotFound();

            var deleted = await personService.DeleteDocumentAsync(id, documentId);
            if (!deleted)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(document.StorageKey))
            {
                try
                {
                    await documentStorage.DeleteAsync(document.StorageKey, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Nao foi possivel remover o arquivo no storage: {StorageKey}", document.StorageKey);
                }
            }

            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao remover documento.");
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            var deleted = await personService.DeleteAsync(id);

            return deleted ? NoContent() : NotFound();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(400, e.Message);
        }
    }

    [HttpPost("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Export([FromBody] ExportPersonsRequestDto dto)
    {
        try
        {
            var persons = await personService.GetForExportAsync(dto.Query, dto.PersonIds ?? []);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Pessoas");

            var headers = new[]
            {
                "Nome",
                "CPF",
                "WhatsApp",
                "Último status",
                "Última atualização do status",
                "NIT Vinculado",
                "Criado em"
            };
            for (var col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            var rowIndex = 2;
            foreach (var person in persons)
            {
                worksheet.Cell(rowIndex, 1).Value = ValueOrDash(person.Name);
                worksheet.Cell(rowIndex, 2).Value = ValueOrDash(person.Cpf);
                worksheet.Cell(rowIndex, 3).Value = ValueOrDash(person.WhatsApp);
                worksheet.Cell(rowIndex, 4).Value = RetirementRequestStatusLabelMapper.ToPtBrLabel(person.RetirementRequestStatus);
                worksheet.Cell(rowIndex, 5).Value = person.RetirementRequestStatusLastEmailUpdatedAt is null
                    ? "-"
                    : person.RetirementRequestStatusLastEmailUpdatedAt.Value.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(rowIndex, 6).Value = ValueOrDash(person.NitId);
                worksheet.Cell(rowIndex, 7).Value = person.CreatedAt == default
                    ? "-"
                    : person.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                rowIndex++;
            }

            var range = worksheet.Range(1, 1, Math.Max(1, persons.Count + 1), headers.Length);
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

            worksheet.Rows(2, Math.Max(2, persons.Count + 1)).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 28);
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 18);
            worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 18);
            worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 30);
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 24);
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 20);
            worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, 18);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"prevly-pessoas-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return StatusCode(500, "Erro ao exportar relatorio de pessoas.");
        }
    }

    public sealed class ExportPersonsRequestDto
    {
        public string? Query { get; init; }
        public List<string>? PersonIds { get; init; }
    }

    public sealed class UploadPersonDocumentRequestDto
    {
        public IFormFile? File { get; set; }
        public PersonDocumentType DocumentType { get; set; } = PersonDocumentType.Other;
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
    }

    public sealed class CreateDocumentUploadUrlRequestDto
    {
        public string FileName { get; init; } = string.Empty;
        public string? ContentType { get; init; }
    }

    public sealed class CreateDocumentUploadUrlResponseDto
    {
        public string StorageKey { get; init; } = string.Empty;
        public string UploadUrl { get; init; } = string.Empty;
    }

    public sealed class CompleteDocumentUploadRequestDto
    {
        public PersonDocumentType DocumentType { get; init; } = PersonDocumentType.Other;
        public string FileName { get; init; } = string.Empty;
        public string StorageKey { get; init; } = string.Empty;
        public string? ContentType { get; init; }
        public string? Description { get; init; }
        public string? CreatedBy { get; init; }
    }

    public sealed class PrivateDocumentViewUrlResponseDto
    {
        public string Url { get; init; } = string.Empty;
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;
}
