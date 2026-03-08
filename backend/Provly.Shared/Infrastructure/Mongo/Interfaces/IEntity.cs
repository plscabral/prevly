namespace Provly.Shared.Infrastructure.Mongo.Interfaces;

public interface IEntity
{
    public string? Id { get; set; }
    public DateTime CreatedAt { get; set; }
}