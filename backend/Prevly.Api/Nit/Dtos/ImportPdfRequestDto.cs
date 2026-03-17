namespace Prevly.Api.Nit.Dtos;

public sealed class ImportPdfRequestDto
{
    public IFormFile? File { get; set; }
    public string? PersonId { get; set; }
}
