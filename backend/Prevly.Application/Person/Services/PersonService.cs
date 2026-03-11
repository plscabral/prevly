using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Services.DTOs.Person;
using Prevly.Domain.Interfaces;
using Provly.Shared.Pagination;

namespace Prevly.Application.Person.Services;

public sealed class PersonService(IPersonRepository personRepository) : IPersonService
{
    public Task<PagedResult<Domain.Entities.Person>> GetPaginatedAsync(FilterPersonDto parameters)
    {
        var filters = new List<FilterDefinition<Domain.Entities.Person>>();

        if (!string.IsNullOrWhiteSpace(parameters.Name))
        {
            var namePattern = Regex.Escape(parameters.Name.Trim());
            filters.Add(Builders<Domain.Entities.Person>.Filter.Regex(person => person.Name, new BsonRegularExpression(namePattern, "i")));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Cpf))
        {
            var cpfPattern = Regex.Escape(parameters.Cpf.Trim());
            filters.Add(Builders<Domain.Entities.Person>.Filter.Regex(person => person.Cpf, new BsonRegularExpression(cpfPattern, "i")));
        }

        var filter = filters.Count > 0
            ? Builders<Domain.Entities.Person>.Filter.And(filters)
            : Builders<Domain.Entities.Person>.Filter.Empty;

        return personRepository.GetPaginatedAsync(filter, parameters);
    }

    public Task<Domain.Entities.Person?> GetByIdAsync(string id) => personRepository.GetByIdAsync(id);

    public async Task<Domain.Entities.Person> CreateAsync(CreatePersonDto request)
    {
        var person = new Domain.Entities.Person
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            Cpf = request.Cpf.Trim(),
            Age = request.Age,
            BirthDate = request.BirthDate,
            CreatedAt = DateTime.UtcNow
        };

        await personRepository.CreateAsync(person);
        return person;
    }

    public async Task<Domain.Entities.Person?> UpdateAsync(string id, UpdatePersonDto dto)
    {
        var existingPerson = await personRepository.GetByIdAsync(id);
        if (existingPerson is null)
            return null;

        existingPerson.Name = dto.Name.Trim();
        existingPerson.Cpf = dto.Cpf.Trim();
        existingPerson.Age = dto.Age;
        existingPerson.BirthDate = dto.BirthDate;

        await personRepository.UpdateAsync(id, existingPerson);
        return existingPerson;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existingPerson = await personRepository.GetByIdAsync(id);
        if (existingPerson is null)
            return false;

        await personRepository.DeleteAsync(id);
        return true;
    }
}
