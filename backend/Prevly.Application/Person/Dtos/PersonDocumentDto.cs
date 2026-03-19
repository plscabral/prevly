using Prevly.Domain.Entities;

namespace Prevly.Application.Services.DTOs.Person;

public sealed class PersonDocumentDto
{
    public string? Id { get; init; }
    public PersonDocumentType DocumentType { get; init; }
    public string? FileName { get; init; }
    public string? Description { get; init; }
    public string? ContentType { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime UploadedAt { get; init; }
}

