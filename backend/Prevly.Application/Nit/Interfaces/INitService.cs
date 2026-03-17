using Prevly.Application.Nit.Dtos;

using Provly.Shared.Pagination;

namespace Prevly.Application.Nit.Interfaces;

public interface INitService
{
    Task<PagedResult<Prevly.Domain.Entities.Nit>> GetPaginatedAsync(
        FilterNitDto dto
    );
    Task<ImportNitsResultDto> ImportFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? personId
    );
    Task<ImportNitsResultDto> ImportFromNumbersAsync(
        IReadOnlyCollection<string> numbers,
        string? personId
    );
    Task<ProcessOwnershipChecksResultDto> ProcessPendingOwnershipChecksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PendingContributionNitDto>> GetPendingContributionCalculationAsync();
    Task<ContributionDetailsImportResultDto> ImportContributionDetailsFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    );
    Task BindPersonToNitAsync(BindPersonToNitDto dto);
    Task<IReadOnlyCollection<NitReportItemDto>> CreateReportAsync(CreateNitReportRequestDto dto);
    Task<IReadOnlyCollection<Prevly.Domain.Entities.Nit>> GetForExportAsync(
        string? query,
        Prevly.Domain.Entities.NitStatus? status,
        IReadOnlyCollection<string>? nitIds
    );
}
