using Prevly.Domain.Entities;

namespace Prevly.WorkerService.Interfaces;

public interface IPersonResolverService
{
    Task<Person> ResolveAsync(string extractedName, string? extractedCpf, CancellationToken cancellationToken);
}
