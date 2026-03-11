namespace Prevly.Application.Services.DTOs.Person;

public class UpdatePersonDto
{
    public required string Name { get; init; }
    public required string Cpf { get; init; }
    public int? Age { get; init; }
    public DateTime? BirthDate { get; init; }
}