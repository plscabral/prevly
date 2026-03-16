namespace Prevly.Api.SocialSecurityRegistration.Dtos;

public sealed class ImportSimpleRequestDto
{
    public string? Number { get; set; }
    public List<string>? Numbers { get; set; }
    public string? PersonId { get; set; }
}
