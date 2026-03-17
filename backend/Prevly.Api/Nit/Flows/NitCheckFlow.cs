using Prevly.Application.Nit.Dtos;
using Prevly.Application.Nit.Interfaces;
using Prevly.Api.Nit.Services;

namespace Prevly.Api.Nit.Flows;

public sealed class NitCheckFlow(INitService nitService)
{
    public async Task<ImportNitsResultDto> ExecuteAsync(
        IReadOnlyCollection<PdfImportDocument> documents,
        string? personId,
        CancellationToken cancellationToken = default
    )
    {
        var totalCandidates = 0;
        var totalValidNits = 0;
        var inserted = 0;
        var duplicates = 0;
        var numbers = new HashSet<string>();

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = new MemoryStream(document.Content, writable: false);
            var result = await nitService.ImportFromPdfAsync(
                stream,
                document.FileName,
                "application/pdf",
                personId
            );

            totalCandidates += result.TotalCandidates;
            totalValidNits += result.TotalValidNits;
            inserted += result.Inserted;
            duplicates += result.Duplicates;

            foreach (var number in result.Numbers)
                numbers.Add(number);
        }

        return new ImportNitsResultDto(
            TotalCandidates: totalCandidates,
            TotalValidNits: totalValidNits,
            Inserted: inserted,
            Duplicates: duplicates,
            Numbers: numbers.ToList()
        );
    }
}
