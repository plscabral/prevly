using Prevly.Domain.Entities;
using Provly.Shared.Infrastructure.Mongo.Interfaces;

namespace Prevly.Domain.Interfaces;

public interface IPersonRepository : IMongoRepository<Person>
{
    Task<Person?> GetByCpfAsync(string cpf);
    Task<Person?> GetByFullNameAsync(string fullName);
}
