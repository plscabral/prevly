using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Entities;

public class SocialSecurityRegistration : IEntity
{
    public string? Id { get; set; }
    public string? Number { get; set; }
    public bool IsAutonomous { get; set; }
    public DateTime FirstContributionDate { get; set; }
    public DateTime LastContributionDate { get; set; }
    public int ContributionYears { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PersonId { get; set; }
    public bool IsUsed { get; set; }
}