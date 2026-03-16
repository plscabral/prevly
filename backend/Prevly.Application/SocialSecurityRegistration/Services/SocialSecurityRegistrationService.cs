using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using Prevly.Domain.Entities;
using Prevly.Domain.Interfaces;
using Provly.Shared.Pagination;
using UglyToad.PdfPig;
using SocialSecurityRegistrationEntity = Prevly.Domain.Entities.SocialSecurityRegistration;

namespace Prevly.Application.SocialSecurityRegistration.Services;

public sealed class SocialSecurityRegistrationService(
    ISocialSecurityRegistrationRepository socialSecurityRegistrationRepository,
    IPersonRepository personRepository,
    INitOwnershipChecker nitOwnershipChecker
) : ISocialSecurityRegistrationService
{
    private static readonly Regex NitRegex = new(@"(?<!\d)(?:\d[\s.\-]?){11}(?!\d)", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"\b\d{2}[/-]\d{2}[/-]\d{4}\b", RegexOptions.Compiled);

    public Task<PagedResult<SocialSecurityRegistrationEntity>> GetPaginatedAsync(FilterSocialSecurityRegistrationDto parameters)
    {
        var filters = new List<FilterDefinition<SocialSecurityRegistrationEntity>>();

        if (!string.IsNullOrWhiteSpace(parameters.Number))
        {
            var numberPattern = Regex.Escape(parameters.Number.Trim());
            filters.Add(Builders<SocialSecurityRegistrationEntity>.Filter.Regex(
                registration => registration.Number,
                new BsonRegularExpression(numberPattern, "i")
            ));
        }

        if (parameters.Status.HasValue)
        {
            filters.Add(Builders<SocialSecurityRegistrationEntity>.Filter.Eq(
                registration => registration.Status,
                parameters.Status.Value
            ));
        }

        if (!string.IsNullOrWhiteSpace(parameters.PersonId))
        {
            filters.Add(Builders<SocialSecurityRegistrationEntity>.Filter.Eq(
                registration => registration.PersonId,
                parameters.PersonId.Trim()
            ));
        }

        var filter = filters.Count > 0
            ? Builders<SocialSecurityRegistrationEntity>.Filter.And(filters)
            : Builders<SocialSecurityRegistrationEntity>.Filter.Empty;

        return socialSecurityRegistrationRepository.GetPaginatedAsync(filter, parameters);
    }

    public async Task<ImportSocialSecurityRegistrationsResultDto> ImportFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? personId
    )
    {
        ValidatePdfFile(pdfStream, fileName, contentType);

        var text = await ExtractTextAsync(pdfStream);
        
         var candidates = ExtractCandidates(text)
            .Distinct()
            .ToList();
         
        return await SaveUniqueValidNitsAsync(candidates, personId);
    }

    public async Task<ImportSocialSecurityRegistrationsResultDto> ImportFromNumbersAsync(
        IReadOnlyCollection<string> numbers,
        string? personId
    )
    {
        if (numbers.Count == 0)
            throw new ArgumentException("Informe ao menos um NIT.");

        var candidates = numbers
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new string(x.Where(char.IsDigit).ToArray()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return await SaveUniqueValidNitsAsync(candidates, personId);
    }

    public async Task<ProcessOwnershipChecksResultDto> ProcessPendingOwnershipChecksAsync(
        CancellationToken cancellationToken = default
    )
    {
        var processed = 0;
        var movedToContributionCalculation = 0;
        var rejectedOwnedByAnotherPerson = 0;
        var errors = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var pendingFilter = Builders<SocialSecurityRegistrationEntity>.Filter.Eq(
                x => x.Status,
                SocialSecurityRegistrationStatus.PendingOwnershipCheck
            );

            var pendingItems = await socialSecurityRegistrationRepository.GetPaginatedAsync(
                pendingFilter,
                new PaginationParameters { PageNumber = 1, PageSize = 200 }
            );

            if (pendingItems.Data.Count == 0)
                break;

            foreach (var item in pendingItems.Data)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (string.IsNullOrWhiteSpace(item.Number) || item.Id is null)
                {
                    errors++;
                    continue;
                }

                item.Status = SocialSecurityRegistrationStatus.OwnershipCheckInProgress;
                item.LastProcessingError = null;
                await socialSecurityRegistrationRepository.UpdateAsync(item.Id, item);

                try
                {
                    var result = await nitOwnershipChecker.CheckAsync(item.Number, cancellationToken);

                    item.OwnershipCheckedAt = DateTime.UtcNow;
                    if (result.BelongsToSomeone)
                    {
                        item.Status = SocialSecurityRegistrationStatus.RejectedOwnedByAnotherPerson;
                        rejectedOwnedByAnotherPerson++;
                    }
                    else
                    {
                        item.Status = SocialSecurityRegistrationStatus.PendingContributionCalculation;
                        movedToContributionCalculation++;
                    }

                    await socialSecurityRegistrationRepository.UpdateAsync(item.Id, item);
                    processed++;
                }
                catch (Exception ex)
                {
                    item.Status = SocialSecurityRegistrationStatus.PendingOwnershipCheck;
                    item.LastProcessingError = ex.Message;
                    await socialSecurityRegistrationRepository.UpdateAsync(item.Id, item);
                    errors++;
                }
            }
        }

        return new ProcessOwnershipChecksResultDto(
            Processed: processed,
            MovedToContributionCalculation: movedToContributionCalculation,
            RejectedOwnedByAnotherPerson: rejectedOwnedByAnotherPerson,
            Errors: errors
        );
    }

    public async Task<IReadOnlyCollection<PendingContributionNitDto>> GetPendingContributionCalculationAsync()
    {
        var filter = Builders<SocialSecurityRegistrationEntity>.Filter.Eq(
            x => x.Status,
            SocialSecurityRegistrationStatus.PendingContributionCalculation
        );

        var pagedResult = await socialSecurityRegistrationRepository.GetPaginatedAsync(
            filter,
            new PaginationParameters { PageNumber = 1, PageSize = 500 }
        );

        return pagedResult.Data
            .Where(x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Number))
            .Select(x => new PendingContributionNitDto(
                Id: x.Id!,
                Number: x.Number!,
                CreatedAt: x.CreatedAt
            ))
            .ToList();
    }

    public async Task<ContributionDetailsImportResultDto> ImportContributionDetailsFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        ValidatePdfFile(pdfStream, fileName, contentType);

        var text = await ExtractTextAsync(pdfStream);
        var nit = ExtractCandidates(text).FirstOrDefault(IsValidNit);
        var dates = ExtractDates(text);

        if (string.IsNullOrWhiteSpace(nit) || dates.Count == 0)
        {
            return new ContributionDetailsImportResultDto(
                ProcessedFiles: 1,
                UpdatedRegistrations: 0,
                NotFoundNits: 0,
                InvalidFiles: 1,
                UpdatedNitNumbers: []
            );
        }

        var filter = Builders<SocialSecurityRegistrationEntity>.Filter.And(
            Builders<SocialSecurityRegistrationEntity>.Filter.Eq(x => x.Number, nit),
            Builders<SocialSecurityRegistrationEntity>.Filter.In(
                x => x.Status,
                [
                    SocialSecurityRegistrationStatus.PendingContributionCalculation,
                    SocialSecurityRegistrationStatus.ReadyForPersonBinding,
                    SocialSecurityRegistrationStatus.BoundToPerson
                ]
            )
        );

        var registration = await socialSecurityRegistrationRepository.GetOneAsync(filter);
        if (registration?.Id is null)
        {
            return new ContributionDetailsImportResultDto(
                ProcessedFiles: 1,
                UpdatedRegistrations: 0,
                NotFoundNits: 1,
                InvalidFiles: 0,
                UpdatedNitNumbers: []
            );
        }

        var firstContribution = dates.Min();
        var lastContribution = dates.Max();

        registration.FirstContributionDate = firstContribution;
        registration.LastContributionDate = lastContribution;
        registration.ContributionYears = CalculateContributionYears(firstContribution, lastContribution);
        registration.Status = registration.PersonId is null
            ? SocialSecurityRegistrationStatus.ReadyForPersonBinding
            : SocialSecurityRegistrationStatus.BoundToPerson;
        registration.LastProcessingError = null;

        await socialSecurityRegistrationRepository.UpdateAsync(registration.Id, registration);

        return new ContributionDetailsImportResultDto(
            ProcessedFiles: 1,
            UpdatedRegistrations: 1,
            NotFoundNits: 0,
            InvalidFiles: 0,
            UpdatedNitNumbers: [nit]
        );
    }

    public async Task BindPersonToNitAsync(BindPersonToNitDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SocialSecurityRegistrationId) || string.IsNullOrWhiteSpace(dto.PersonId))
            throw new ArgumentException("SocialSecurityRegistrationId e PersonId sao obrigatorios.");

        var person = await personRepository.GetByIdAsync(dto.PersonId);
        if (person is null)
            throw new ArgumentException("Person nao encontrada.");

        var registration = await socialSecurityRegistrationRepository.GetByIdAsync(dto.SocialSecurityRegistrationId);
        if (registration?.Id is null)
            throw new ArgumentException("NIT nao encontrado.");

        if (registration.Status != SocialSecurityRegistrationStatus.ReadyForPersonBinding &&
            registration.Status != SocialSecurityRegistrationStatus.BoundToPerson)
        {
            throw new InvalidOperationException("NIT ainda nao esta pronto para vinculo com person.");
        }

        registration.PersonId = person.Id;
        registration.Status = SocialSecurityRegistrationStatus.BoundToPerson;

        await socialSecurityRegistrationRepository.UpdateAsync(registration.Id, registration);
    }

    public async Task<IReadOnlyCollection<NitReportItemDto>> CreateReportAsync(CreateNitReportRequestDto dto)
    {
        if (dto.RegistrationIds.Count == 0)
            return [];

        var reportItems = new List<NitReportItemDto>(dto.RegistrationIds.Count);

        foreach (var registrationId in dto.RegistrationIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            var registration = await socialSecurityRegistrationRepository.GetByIdAsync(registrationId);
            if (registration?.Number is null)
                continue;

            Domain.Entities.Person? person = null;
            if (!string.IsNullOrWhiteSpace(registration.PersonId))
                person = await personRepository.GetByIdAsync(registration.PersonId);

            reportItems.Add(new NitReportItemDto(
                NitNumber: registration.Number,
                PersonId: person?.Id,
                PersonName: person?.Name,
                PersonCpf: person?.Cpf,
                ContributionYears: registration.ContributionYears
            ));
        }

        return reportItems;
    }

    #region Private methods

    private async Task<ImportSocialSecurityRegistrationsResultDto> SaveUniqueValidNitsAsync(
        IReadOnlyCollection<string> candidates,
        string? personId
    )
    {
        var validNits = candidates.Where(IsValidNit).Distinct().ToList();

        var inserted = 0;
        var duplicates = 0;

        foreach (var number in validNits)
        {
            var filter = Builders<SocialSecurityRegistrationEntity>.Filter.Eq(x => x.Number, number);
            var existing = await socialSecurityRegistrationRepository.GetOneAsync(filter);

            if (existing is not null)
            {
                duplicates++;
                continue;
            }

            var registration = new SocialSecurityRegistrationEntity
            {
                Number = number,
                PersonId = personId,
                CreatedAt = DateTime.UtcNow,
                FirstContributionDate = DateTime.MinValue,
                LastContributionDate = DateTime.MinValue,
                ContributionYears = 0,
                Status = SocialSecurityRegistrationStatus.PendingOwnershipCheck,
                LastProcessingError = null,
                OwnershipCheckedAt = null
            };

            await socialSecurityRegistrationRepository.CreateAsync(registration);
            inserted++;
        }

        return new ImportSocialSecurityRegistrationsResultDto(
            TotalCandidates: candidates.Count,
            TotalValidNits: validNits.Count,
            Inserted: inserted,
            Duplicates: duplicates,
            Numbers: validNits
        );
    }

    private static void ValidatePdfFile(Stream pdfStream, string fileName, string contentType)
    {
        if (pdfStream is null)
            throw new ArgumentException("O arquivo PDF e obrigatorio.");

        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Dados do arquivo invalido.");

        if (!contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("O arquivo enviado precisa ser um PDF.");
        }
    }

    private static async Task<string> ExtractTextAsync(Stream pdfStream)
    {
        await using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream);
        var pdfBytes = memoryStream.ToArray();
        var nativeText = ExtractNativePdfText(pdfBytes);

        // Fast path: if native text already has NIT candidates, no OCR needed.
        if (ExtractCandidates(nativeText).Count > 0)
            return nativeText;

        var ocrText = await TryExtractTextWithOcrAsync(pdfBytes);
        return string.Join(
            Environment.NewLine,
            new[] { nativeText, ocrText }.Where(x => !string.IsNullOrWhiteSpace(x))
        );
    }

    private static string ExtractNativePdfText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var pdf = PdfDocument.Open(stream);
        return string.Join(Environment.NewLine, pdf.GetPages().Select(page => page.Text));
    }

    private static async Task<string> TryExtractTextWithOcrAsync(byte[] pdfBytes)
    {
        var hasPdfToPpm = await IsCommandAvailableAsync("pdftoppm", "-h");
        var hasTesseract = await IsCommandAvailableAsync("tesseract", "--version");

        if (!hasPdfToPpm || !hasTesseract)
            return string.Empty;

        var tempRoot = Path.Combine(Path.GetTempPath(), $"prevly-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var inputPdfPath = Path.Combine(tempRoot, "input.pdf");
            await File.WriteAllBytesAsync(inputPdfPath, pdfBytes);

            var outputPrefix = Path.Combine(tempRoot, "page");
            var render = await RunProcessAsync(
                "pdftoppm",
                $"-f 1 -l 12 -r 300 -png \"{inputPdfPath}\" \"{outputPrefix}\"",
                timeoutSeconds: 120
            );

            if (render.ExitCode != 0)
                return string.Empty;

            var images = Directory.GetFiles(tempRoot, "page-*.png")
                .OrderBy(x => x)
                .ToList();

            if (images.Count == 0)
                return string.Empty;

            var allText = new StringBuilder();

            foreach (var imagePath in images)
            {
                // Multiple PSM modes improve extraction for tabular and vertical text.
                foreach (var args in new[]
                         {
                             $"\"{imagePath}\" stdout -l por+eng --psm 6",
                             $"\"{imagePath}\" stdout -l por+eng --psm 11",
                             $"\"{imagePath}\" stdout -l por+eng --psm 5 -c textord_tabfind_vertical_text=1"
                         })
                {
                    var ocr = await RunProcessAsync("tesseract", args, timeoutSeconds: 90);
                    if (ocr.ExitCode == 0 && !string.IsNullOrWhiteSpace(ocr.StdOut))
                    {
                        allText.AppendLine(ocr.StdOut);
                    }
                }
            }

            return allText.ToString();
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }

    private static async Task<bool> IsCommandAvailableAsync(string command, string args)
    {
        try
        {
            var result = await RunProcessAsync(command, args, timeoutSeconds: 20);
            return result.ExitCode == 0 || !string.IsNullOrWhiteSpace(result.StdOut) || !string.IsNullOrWhiteSpace(result.StdErr);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        string arguments,
        int timeoutSeconds
    )
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

        var completed = await Task.WhenAny(waitTask, timeoutTask);
        if (completed == timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore kill failures
            }

            throw new TimeoutException($"Processo '{fileName}' excedeu o timeout de {timeoutSeconds}s.");
        }

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;
        return (process.ExitCode, stdOut, stdErr);
    }

    private static List<string> ExtractCandidates(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var matches = NitRegex.Matches(text);
        var result = new List<string>(matches.Count);

        foreach (Match match in matches)
        {
            var digitsOnly = new string(match.Value.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length == 11)
                result.Add(digitsOnly);
        }

        return result;
    }

    private static List<DateTime> ExtractDates(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var dates = new List<DateTime>();
        var matches = DateRegex.Matches(text);

        foreach (Match match in matches)
        {
            var value = match.Value;
            if (DateTime.TryParseExact(
                    value,
                    ["dd/MM/yyyy", "dd-MM-yyyy"],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                dates.Add(parsed);
            }
        }

        return dates;
    }

    private static int CalculateContributionYears(DateTime firstContribution, DateTime lastContribution)
    {
        if (lastContribution < firstContribution)
            return 0;

        var years = lastContribution.Year - firstContribution.Year;
        if (lastContribution < firstContribution.AddYears(years))
            years--;

        return Math.Max(years, 0);
    }

    private static bool IsValidNit(string nit)
    {
        if (string.IsNullOrWhiteSpace(nit) || nit.Length != 11 || nit.Any(c => !char.IsDigit(c)))
            return false;

        var weights = new[] { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum = 0;

        for (var i = 0; i < 10; i++)
            sum += (nit[i] - '0') * weights[i];

        var remainder = sum % 11;
        var expectedDigit = remainder < 2 ? 0 : 11 - remainder;
        var actualDigit = nit[10] - '0';

        return actualDigit == expectedDigit;
    }

    #endregion
}
