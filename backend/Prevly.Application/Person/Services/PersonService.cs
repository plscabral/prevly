using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Prevly.Application.Person.Interfaces;
using Prevly.Application.Services.DTOs.Person;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Pagination;

namespace Prevly.Application.Person.Services;

public sealed class PersonService(
    IPersonRepository personRepository,
    IMonitoredEmailRepository monitoredEmailRepository
) : IPersonService
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

        var sort = Builders<Domain.Entities.Person>.Sort
            .Ascending(person => person.Name)
            .Ascending(person => person.Id);

        return personRepository.GetPaginatedAsync(filter, parameters, sort);
    }

    public Task<Domain.Entities.Person?> GetByIdAsync(string id) => personRepository.GetByIdAsync(id);

    public async Task<PersonDetailsDto?> GetDetailsAsync(string id)
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return null;

        var monitoredEmails = await monitoredEmailRepository.GetByPersonIdAsync(id);
        var mappedEmails = monitoredEmails
            .OrderByDescending(x => x.ReceivedAt)
            .Select(x => new MonitoredEmailDto
            {
                Id = x.Id,
                PersonId = x.PersonId,
                Subject = x.Subject,
                From = x.From,
                RawContent = x.RawContent,
                Summary = x.Summary,
                ReceivedAt = x.ReceivedAt,
                IdentifiedStatus = x.IdentifiedStatus,
                IdentifiedStatusLabel = RetirementRequestStatusLabelMapper.ToPtBrLabel(x.IdentifiedStatus),
                ExtractedName = x.ExtractedName,
                ExtractedCpf = x.ExtractedCpf,
                ExtractedBenefitNumber = x.ExtractedBenefitNumber,
                MessageUniqueId = x.MessageUniqueId,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var financialEntries = (person.FinancialEntries ?? [])
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PersonFinancialEntryDto
            {
                Id = x.Id,
                Type = x.Type,
                Description = x.Description,
                Value = x.Value,
                Date = x.Date,
                Origin = x.Origin,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var documents = (person.Documents ?? [])
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new PersonDocumentDto
            {
                Id = x.Id,
                DocumentType = x.DocumentType,
                FileName = x.FileName,
                Description = x.Description,
                ContentType = x.ContentType,
                CreatedBy = x.CreatedBy,
                UploadedAt = x.UploadedAt
            })
            .ToList();

        var mappedAgreement = MapAgreement(person.RetirementAgreement);
        var summary = ComputeFinancialSummary(person.RetirementAgreement, person.FinancialEntries);

        return new PersonDetailsDto
        {
            Person = person,
            MonitoredEmails = mappedEmails,
            RetirementAgreement = mappedAgreement,
            FinancialEntries = financialEntries,
            Documents = documents,
            FinancialSummary = summary
        };
    }

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
            Name = NormalizePersonName(request.Name),
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

        existingPerson.Name = NormalizePersonName(dto.Name);
        existingPerson.Cpf = dto.Cpf.Trim();
        existingPerson.Phone = dto.Phone?.Trim();
        existingPerson.WhatsApp = dto.WhatsApp?.Trim();
        existingPerson.GovPassword = dto.GovPassword?.Trim();
        existingPerson.Age = dto.Age;
        existingPerson.BirthDate = dto.BirthDate;

        await personRepository.UpdateAsync(id, existingPerson);
        return existingPerson;
    }

    public async Task<Domain.Entities.Person?> UpdateRetirementAgreementAsync(
        string id,
        UpsertPersonRetirementAgreementDto dto
    )
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return null;

        person.RetirementAgreement = new PersonRetirementAgreement
        {
            TotalCost = dto.TotalCost,
            OperationalCostType = dto.OperationalCostType,
            OperationalCostSimpleValue = dto.OperationalCostSimpleValue,
            OperationalCostItems = (dto.OperationalCostItems ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.Description) || x.Value != 0)
                .Select(x => new PersonOperationalCostItem
                {
                    Id = string.IsNullOrWhiteSpace(x.Id) ? Guid.NewGuid().ToString("N") : x.Id!,
                    Description = x.Description?.Trim() ?? string.Empty,
                    Value = x.Value
                })
                .ToList(),
            MonthlyRetirementValue = dto.MonthlyRetirementValue,
            PaymentType = dto.PaymentType,
            HasDownPayment = dto.HasDownPayment,
            DownPaymentValue = dto.DownPaymentValue,
            DownPaymentDate = dto.DownPaymentDate,
            DiscountFromBenefit = dto.DiscountFromBenefit,
            MonthlyAmountForSettlement = dto.MonthlyAmountForSettlement,
            FinancialNotes = dto.FinancialNotes?.Trim()
        };

        await personRepository.UpdateAsync(id, person);
        return person;
    }

    public async Task<PersonFinancialEntry?> AddFinancialEntryAsync(string id, AddPersonFinancialEntryDto dto)
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return null;

        person.FinancialEntries ??= [];
        var entry = new PersonFinancialEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = dto.Type,
            Description = dto.Description?.Trim(),
            Value = dto.Value,
            Date = dto.Date,
            Origin = dto.Origin?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        person.FinancialEntries.Add(entry);
        await personRepository.UpdateAsync(id, person);
        return entry;
    }

    public async Task<bool> DeleteFinancialEntryAsync(string id, string entryId)
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return false;

        person.FinancialEntries ??= [];
        var removed = person.FinancialEntries.RemoveAll(x => x.Id == entryId) > 0;
        if (!removed)
            return false;

        await personRepository.UpdateAsync(id, person);
        return true;
    }

    public async Task<PersonDocument?> AddDocumentAsync(string id, AddPersonDocumentDto dto)
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return null;

        person.Documents ??= [];
        var document = new PersonDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            DocumentType = dto.DocumentType,
            FileName = dto.FileName.Trim(),
            StorageKey = dto.StorageKey.Trim(),
            ContentType = dto.ContentType?.Trim(),
            Description = dto.Description?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim(),
            UploadedAt = dto.UploadedAt
        };

        person.Documents.Add(document);
        await personRepository.UpdateAsync(id, person);
        return document;
    }

    public async Task<PersonDocument?> GetDocumentAsync(string id, string documentId)
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return null;

        person.Documents ??= [];
        return person.Documents.FirstOrDefault(x => x.Id == documentId);
    }

    public async Task<bool> DeleteDocumentAsync(string id, string documentId)
    {
        var person = await personRepository.GetByIdAsync(id);
        if (person is null)
            return false;

        person.Documents ??= [];
        var removed = person.Documents.RemoveAll(x => x.Id == documentId) > 0;
        if (!removed)
            return false;

        await personRepository.UpdateAsync(id, person);
        return true;
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
        var sort = Builders<Domain.Entities.Person>.Sort
            .Ascending(person => person.Name)
            .Ascending(person => person.Id);
        var pageNumber = 1;
        var all = new List<Domain.Entities.Person>();

        while (true)
        {
            var page = await personRepository.GetPaginatedAsync(
                filter,
                new PaginationParameters { PageNumber = pageNumber, PageSize = pageSize },
                sort
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

    private static string NormalizePersonName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var collapsed = string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return collapsed.ToUpperInvariant();
    }

    private static PersonRetirementAgreementDto? MapAgreement(PersonRetirementAgreement? agreement)
    {
        if (agreement is null)
            return null;

        return new PersonRetirementAgreementDto
        {
            TotalCost = agreement.TotalCost,
            OperationalCostType = agreement.OperationalCostType,
            OperationalCostSimpleValue = agreement.OperationalCostSimpleValue,
            OperationalCostItems = (agreement.OperationalCostItems ?? [])
                .Select(x => new PersonOperationalCostItemDto
                {
                    Id = x.Id,
                    Description = x.Description,
                    Value = x.Value
                })
                .ToList(),
            MonthlyRetirementValue = agreement.MonthlyRetirementValue,
            PaymentType = agreement.PaymentType,
            HasDownPayment = agreement.HasDownPayment,
            DownPaymentValue = agreement.DownPaymentValue,
            DownPaymentDate = agreement.DownPaymentDate,
            DiscountFromBenefit = agreement.DiscountFromBenefit,
            MonthlyAmountForSettlement = agreement.MonthlyAmountForSettlement,
            FinancialNotes = agreement.FinancialNotes
        };
    }

    private static PersonFinancialSummaryDto? ComputeFinancialSummary(
        PersonRetirementAgreement? agreement,
        IReadOnlyCollection<PersonFinancialEntry>? entries
    )
    {
        if (agreement is null && (entries is null || entries.Count == 0))
            return null;

        var operationalCostTotal = agreement?.OperationalCostType == PersonOperationalCostType.Detailed
            ? (agreement.OperationalCostItems ?? []).Sum(x => x.Value)
            : (agreement?.OperationalCostSimpleValue ?? 0m);

        var totalPaid = (entries ?? []).Sum(x => x.Value);
        var totalCost = agreement?.TotalCost;
        var totalOpen = totalCost.HasValue ? totalCost.Value - totalPaid : (decimal?)null;
        var monthlyRetirement = agreement?.MonthlyRetirementValue;
        var monthlySettlement = agreement?.MonthlyAmountForSettlement;

        decimal? salaryCount = null;
        if (totalOpen.HasValue && monthlyRetirement.HasValue && monthlyRetirement.Value > 0)
        {
            salaryCount = decimal.Round(totalOpen.Value / monthlyRetirement.Value, 2);
        }

        decimal? installmentCount = null;
        if (totalOpen.HasValue && monthlySettlement.HasValue && monthlySettlement.Value > 0)
        {
            installmentCount = decimal.Round(totalOpen.Value / monthlySettlement.Value, 2);
        }

        decimal? clientNet = null;
        if (monthlyRetirement.HasValue && monthlySettlement.HasValue)
        {
            clientNet = monthlyRetirement.Value - monthlySettlement.Value;
        }

        return new PersonFinancialSummaryDto
        {
            OperationalCostTotal = operationalCostTotal,
            TotalPaid = totalPaid,
            TotalOpen = totalOpen,
            OutstandingBalance = totalOpen,
            EstimatedSalaryCountToSettle = salaryCount,
            EstimatedInstallmentsToSettle = installmentCount,
            ClientNetMonthlyValue = clientNet
        };
    }
}
