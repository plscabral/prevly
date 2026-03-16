using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Entities;

public class Account : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public DateTime CreatedAt { get; set; }
}