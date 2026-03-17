using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Services.DTOs.Person;
using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(
    ILogger<PersonController> logger,
    IPersonService personService
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

            var headers = new[] { "Nome", "CPF", "WhatsApp", "NIT Vinculado", "Criado em" };
            for (var col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            var rowIndex = 2;
            foreach (var person in persons)
            {
                worksheet.Cell(rowIndex, 1).Value = person.Name ?? string.Empty;
                worksheet.Cell(rowIndex, 2).Value = person.Cpf ?? string.Empty;
                worksheet.Cell(rowIndex, 3).Value = person.WhatsApp ?? string.Empty;
                worksheet.Cell(rowIndex, 4).Value = person.SocialSecurityRegistrationId ?? string.Empty;
                worksheet.Cell(rowIndex, 5).Value = person.CreatedAt.ToString("dd/MM/yyyy HH:mm");
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
            worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 20);
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 18);

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
}
