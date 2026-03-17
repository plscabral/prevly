using Prevly.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Prevly.Domain.Interfaces;
using Provly.Shared.Infrastructure.Mongo.Repositories;

namespace Prevly.Infrastructure;

public sealed class PersonRepository : MongoRepository<Person>, IPersonRepository
{
    private readonly IMongoCollection<Person> _collection;

    public PersonRepository(IMongoDatabase database)
        : base(database, nameof(Person))
    {
        _collection = database.GetCollection<Person>(nameof(Person));
        EnsureIndexes();
    }

    public async Task<Person?> GetByCpfAsync(string cpf)
    {
        var normalizedCpf = NormalizeCpf(cpf);
        if (string.IsNullOrWhiteSpace(normalizedCpf))
            return null;

        var cpfPattern = BuildCpfRegex(normalizedCpf);
        var filter = Builders<Person>.Filter.Regex(x => x.Cpf, cpfPattern);

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Person?> GetByFullNameAsync(string fullName)
    {
        var incomingNameUpper = NormalizeNameForComparison(fullName);
        if (string.IsNullOrWhiteSpace(incomingNameUpper))
            return null;

        var nameRegex = BuildExactNameRegex(incomingNameUpper);
        var filter = Builders<Person>.Filter.Regex(x => x.Name, nameRegex);
        var candidates = await _collection.Find(filter).ToListAsync();

        return candidates.FirstOrDefault(x =>
            NormalizeNameForComparison(x.Name) == incomingNameUpper
        );
    }

    private void EnsureIndexes()
    {
        var cpfIndex = new CreateIndexModel<Person>(
            Builders<Person>.IndexKeys.Ascending(x => x.Cpf),
            new CreateIndexOptions { Name = "idx_person_cpf" }
        );

        _collection.Indexes.CreateOne(cpfIndex);
    }

    private static string NormalizeCpf(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    private static string NormalizeNameForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var collapsed = string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return collapsed.ToUpperInvariant();
    }

    private static BsonRegularExpression BuildCpfRegex(string normalizedCpf)
    {
        var pattern = string.Join(@"\D*", normalizedCpf.ToCharArray().Select(x => x.ToString()));
        return new BsonRegularExpression(pattern);
    }

    private static BsonRegularExpression BuildExactNameRegex(string nameUpper)
    {
        var tokens = nameUpper
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(Regex.Escape);

        var pattern = $"^\\s*{string.Join("\\s+", tokens)}\\s*$";
        return new BsonRegularExpression(pattern, "i");
    }
}
