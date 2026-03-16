using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Entities;

public class SocialSecurityRegistration : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string? Number { get; set; }
    public DateTime? FirstContributionDate { get; set; }
    public DateTime? LastContributionDate { get; set; }
    public int ContributionYears { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PersonId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public SocialSecurityRegistrationStatus Status { get; set; }
    public DateTime? OwnershipCheckedAt { get; set; }
    public string? LastProcessingError { get; set; }
}
