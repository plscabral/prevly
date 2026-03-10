using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Prevly.Application.Services;
using Prevly.Application.Services.Models;
using Prevly.Domain.Entities;
using Provly.Shared.Pagination;

namespace Prevly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController(IPersonService personService) : AuthorizeController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Person>>> GetPaginated([FromQuery] PersonPaginationParameters paginationParameters)
    {
        var result = await personService.GetPaginatedAsync(new Prevly.Application.Services.Models.PersonPaginationParameters
        {
            PageNumber = paginationParameters.PageNumber,
            PageSize = paginationParameters.PageSize,
            Name = paginationParameters.Name,
            Cpf = paginationParameters.Cpf
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Person>> GetById([FromRoute] string id)
    {
        var person = await personService.GetByIdAsync(id);

        return person is null ? NotFound() : Ok(person);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<Person>> Create([FromBody] CreatePersonApiRequest request)
    {
        var person = await personService.CreateAsync(new CreatePersonRequest
        {
            Name = request.Name.Trim(),
            Cpf = request.Cpf.Trim(),
            Age = request.Age,
            BirthDate = request.BirthDate
        });

        return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Person>> Update([FromRoute] string id, [FromBody] UpdatePersonApiRequest request)
    {
        var person = await personService.UpdateAsync(id, new UpdatePersonRequest
        {
            Name = request.Name.Trim(),
            Cpf = request.Cpf.Trim(),
            Age = request.Age,
            BirthDate = request.BirthDate
        });

        return person is null ? NotFound() : Ok(person);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        var deleted = await personService.DeleteAsync(id);

        return deleted ? NoContent() : NotFound();
    }
}

public sealed class CreatePersonApiRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Cpf { get; set; } = string.Empty;

    [Range(0, 150)]
    public int? Age { get; set; }

    public DateTime? BirthDate { get; set; }
}

public sealed class UpdatePersonApiRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Cpf { get; set; } = string.Empty;

    [Range(0, 150)]
    public int? Age { get; set; }

    public DateTime? BirthDate { get; set; }
}

public sealed class PersonPaginationParameters : PaginationParameters
{
    public string? Name { get; set; }
    public string? Cpf { get; set; }
}
