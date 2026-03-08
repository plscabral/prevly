using MongoDB.Driver;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Infrastructure.Mongo.Repositories;

namespace Prevly.Infrastructure;

public class AccountRepository(IMongoDatabase database) : 
    MongoRepository<Account>(database, nameof(Account)), 
    IAccountRepository;