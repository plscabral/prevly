using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Entities;

public class MonitoredEmail : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRequired]
    public string PersonId { get; set; } = string.Empty;

    public string? Subject { get; set; }
    public string? From { get; set; }
    public string? RawContent { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    [BsonRepresentation(BsonType.String)]
    public RetirementRequestStatus? IdentifiedStatus { get; set; }

    public string? ExtractedName { get; set; }
    public string? ExtractedCpf { get; set; }
    public string? ExtractedBenefitNumber { get; set; }
    public string? MessageUniqueId { get; set; }
    public string? ContentHash { get; set; }
    public DateTime CreatedAt { get; set; }
}
