using Prevly.Domain.Entities;
using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Interfaces;

public interface ISocialSecurityRegistrationRepository : IMongoRepository<SocialSecurityRegistration>;