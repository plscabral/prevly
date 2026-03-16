namespace Prevly.Application.Services.DTOs.Person;

public class CreatePersonDto
{
    public required string Name { get; init; }
    public required string Cpf { get; init; }
    public string? Phone { get; init; }
    public string? WhatsApp { get; init; }
    public string? GovPassword { get; init; }
    public int? Age { get; init; }
    public DateTime? BirthDate { get; init; }
}
