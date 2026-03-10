namespace Prevly.Application.Services.Models;

public sealed class UpdatePersonRequest
{
    public required string Name { get; init; }
    public required string Cpf { get; init; }
    public int? Age { get; init; }
    public DateTime? BirthDate { get; init; }
}
