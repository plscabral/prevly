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

    public async Task<IReadOnlyCollection<Domain.Entities.Person>> GetForExportAsync(
        string? query,
        IReadOnlyCollection<string>? personIds
    )
    {
        var filterBuilder = Builders<Domain.Entities.Person>.Filter;
        var filters = new List<FilterDefinition<Domain.Entities.Person>>();

        var selectedIds = personIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList() ?? [];

        if (selectedIds.Count > 0)
        {
            filters.Add(filterBuilder.In(person => person.Id, selectedIds));
        }
        else if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = new BsonRegularExpression(Regex.Escape(query.Trim()), "i");
            filters.Add(filterBuilder.Or(
                filterBuilder.Regex(person => person.Name, pattern),
                filterBuilder.Regex(person => person.Cpf, pattern)
            ));
        }

        var filter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        return await GetAllByFilterAsync(filter);
    }

    public async Task<Domain.Entities.Person> CreateAsync(CreatePersonDto request)
    {
        var person = new Domain.Entities.Person
        {
            Name = request.Name.Trim(),
            Cpf = request.Cpf.Trim(),
            Phone = request.Phone?.Trim(),
            WhatsApp = request.WhatsApp?.Trim(),
            GovPassword = request.GovPassword?.Trim(),
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
        existingPerson.Phone = dto.Phone?.Trim();
        existingPerson.WhatsApp = dto.WhatsApp?.Trim();
        existingPerson.GovPassword = dto.GovPassword?.Trim();
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

    private async Task<IReadOnlyCollection<Domain.Entities.Person>> GetAllByFilterAsync(
        FilterDefinition<Domain.Entities.Person> filter
    )
    {
        const int pageSize = 500;
        var pageNumber = 1;
        var all = new List<Domain.Entities.Person>();

        while (true)
        {
            var page = await personRepository.GetPaginatedAsync(
                filter,
                new PaginationParameters { PageNumber = pageNumber, PageSize = pageSize }
            );

            if (page.Data.Count == 0)
                break;

            all.AddRange(page.Data);

            if (all.Count >= page.TotalRecords)
                break;

            pageNumber++;
        }

        return all;
    }
}
