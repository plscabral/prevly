using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Prevly.WorkerService.Interfaces;

namespace Prevly.WorkerService.Services;

public sealed class PersonResolverService(
    IPersonRepository personRepository,
    ILogger<PersonResolverService> logger
) : IPersonResolverService
{
    public async Task<Person> ResolveAsync(string extractedName, string? extractedCpf, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedName = NormalizeName(extractedName);
        var normalizedCpf = NormalizeCpf(extractedCpf);

        Person? existingPerson = null;
        if (!string.IsNullOrWhiteSpace(normalizedCpf))
        {
            existingPerson = await personRepository.GetByCpfAsync(normalizedCpf);
        }

        if (existingPerson is null && !string.IsNullOrWhiteSpace(normalizedName))
        {
            existingPerson = await personRepository.GetByFullNameAsync(normalizedName);
        }

        if (existingPerson is not null)
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existingPerson.Name) && !string.IsNullOrWhiteSpace(normalizedName))
            {
                existingPerson.Name = normalizedName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existingPerson.Cpf) && !string.IsNullOrWhiteSpace(normalizedCpf))
            {
                existingPerson.Cpf = normalizedCpf;
                changed = true;
            }

            if (changed)
            {
                await personRepository.UpdateAsync(existingPerson.Id!, existingPerson);
            }

            return existingPerson;
        }

        var newPerson = new Person
        {
            Name = normalizedName,
            Cpf = string.IsNullOrWhiteSpace(normalizedCpf) ? null : normalizedCpf,
            CreatedAt = DateTime.UtcNow
        };

        await personRepository.CreateAsync(newPerson);

        logger.LogInformation(
            "Pessoa criada automaticamente via monitoramento de e-mail. PersonId={PersonId} Cpf={Cpf}",
            newPerson.Id,
            newPerson.Cpf
        );

        return newPerson;
    }

    private static string NormalizeCpf(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    private static string NormalizeName(string value) =>
        string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
}
