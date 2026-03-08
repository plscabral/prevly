using Prevly.Domain.Interfaces;
using Prevly.Domain.Entities;
using System.Collections.Concurrent;
using MongoDB.Driver;
using Provly.Shared.Infrastructure.Mongo.Repositories;

namespace Prevly.Infrastructure;

public class PersonRepository(IMongoDatabase database) : 
    MongoRepository<Person>(database, nameof(Person)), 
    IPersonRepository;
