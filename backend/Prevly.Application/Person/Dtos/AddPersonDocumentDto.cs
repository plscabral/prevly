using Prevly.Domain.Entities;

namespace Prevly.Application.Services.DTOs.Person;

public sealed class AddPersonDocumentDto
{
    public PersonDocumentType DocumentType { get; init; } = PersonDocumentType.Other;
    public string FileName { get; init; } = string.Empty;
    public string StorageKey { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public string? Description { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}

