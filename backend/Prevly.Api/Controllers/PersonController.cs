using Microsoft.AspNetCore.Mvc;
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
}
