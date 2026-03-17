using MongoDB.Driver;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Infrastructure.Mongo.Repositories;

namespace Prevly.Infrastructure;

public class NitRepository(IMongoDatabase database) : 
    MongoRepository<Nit>(database, nameof(Nit)), 
    INitRepository;