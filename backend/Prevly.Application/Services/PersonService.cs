using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Prevly.Application.Services.Models;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Pagination;

namespace Prevly.Application.Services;

public sealed class PersonService(IPersonRepository personRepository) : IPersonService
{
    public Task<PagedResult<Person>> GetPaginatedAsync(PersonPaginationParameters parameters)
    {
        var filters = new List<FilterDefinition<Person>>();

        if (!string.IsNullOrWhiteSpace(parameters.Name))
        {
            var namePattern = Regex.Escape(parameters.Name.Trim());
            filters.Add(
                Builders<Person>.Filter.Regex(
                    person => person.Name,
                    new BsonRegularExpression(namePattern, "i")));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Cpf))
        {
            var cpfPattern = Regex.Escape(parameters.Cpf.Trim());
            filters.Add(
                Builders<Person>.Filter.Regex(
                    person => person.Cpf,
                    new BsonRegularExpression(cpfPattern, "i")));
        }

        var filter = filters.Count > 0
            ? Builders<Person>.Filter.And(filters)
            : Builders<Person>.Filter.Empty;

        return personRepository.GetPaginatedAsync(filter, parameters);
    }

    public Task<Person?> GetByIdAsync(string id) => personRepository.GetByIdAsync(id);

    public async Task<Person> CreateAsync(CreatePersonRequest request)
    {
        var person = new Person
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

    public async Task<Person?> UpdateAsync(string id, UpdatePersonRequest request)
    {
        var existingPerson = await personRepository.GetByIdAsync(id);
        if (existingPerson is null)
            return null;

        existingPerson.Name = request.Name.Trim();
        existingPerson.Cpf = request.Cpf.Trim();
        existingPerson.Age = request.Age;
        existingPerson.BirthDate = request.BirthDate;

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
