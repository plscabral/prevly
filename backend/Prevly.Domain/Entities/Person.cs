using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Entities;

public class Person : IEntity
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Cpf { get; set; }
    public int? Age { get; set; }
    
    public string? GovPassword { get; set; }
    public string? SocialSecurityRegistrationId { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime CreatedAt { get; set; }
}