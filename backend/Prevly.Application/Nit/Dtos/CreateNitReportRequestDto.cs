namespace Prevly.Application.Nit.Dtos;

public sealed record CreateNitReportRequestDto(IReadOnlyCollection<string> NitIds);
