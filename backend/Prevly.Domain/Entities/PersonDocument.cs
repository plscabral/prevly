namespace Prevly.Domain.Entities;

public class PersonDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public PersonDocumentType DocumentType { get; set; } = PersonDocumentType.Other;
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

