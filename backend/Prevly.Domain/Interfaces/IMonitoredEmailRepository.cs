using Prevly.Domain.Entities;
using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Interfaces;

public interface IMonitoredEmailRepository : IMongoRepository<MonitoredEmail>
{
    Task<bool> ExistsByMessageUniqueIdAsync(string messageUniqueId);
    Task<bool> ExistsByContentHashAsync(string contentHash);
    Task<IReadOnlyCollection<MonitoredEmail>> GetByPersonIdAsync(string personId);
}
