using MongoDB.Driver;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Infrastructure.Mongo.Repositories;

namespace Prevly.Infrastructure;

public class SocialSecurityRegistrationRepository(IMongoDatabase database) : 
    MongoRepository<SocialSecurityRegistration>(database, nameof(SocialSecurityRegistration)), 
    ISocialSecurityRegistrationRepository;