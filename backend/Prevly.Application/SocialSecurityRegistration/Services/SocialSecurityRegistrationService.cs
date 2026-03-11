using System.Text.RegularExpressions;
using MongoDB.Driver;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Interfaces;
using Prevly.Domain.Interfaces;
using UglyToad.PdfPig;

namespace Prevly.Application.SocialSecurityRegistration.Services;

public sealed class SocialSecurityRegistrationService(
    ISocialSecurityRegistrationRepository socialSecurityRegistrationRepository
) : ISocialSecurityRegistrationService
{
    private static readonly Regex NitRegex = new(@"(?<!\d)(?:\d[\s.\-]?){11}(?!\d)", RegexOptions.Compiled);

    public async Task<ImportSocialSecurityRegistrationsResultDto> ImportFromPdfAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? personId
    )
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

        var text = await ExtractTextAsync(pdfStream);
        var candidates = ExtractCandidates(text);
        var validNits = candidates.Where(IsValidNit).Distinct().ToList();

        var inserted = 0;
        var duplicates = 0;

        foreach (var number in validNits)
        {
            var filter = Builders<Domain.Entities.SocialSecurityRegistration>.Filter.And(
                Builders<Domain.Entities.SocialSecurityRegistration>.Filter.Eq(x => x.Number, number),
                Builders<Domain.Entities.SocialSecurityRegistration>.Filter.Eq(x => x.PersonId, personId)
            );

            var existing = await socialSecurityRegistrationRepository.GetOneAsync(filter);
            
            if (existing is not null)
            {
                duplicates++;
                continue;
            }

            var registration = new Domain.Entities.SocialSecurityRegistration
            {
                Id = Guid.NewGuid().ToString("N"),
                Number = number,
                PersonId = personId,
                IsAutonomous = false,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                FirstContributionDate = DateTime.MinValue,
                LastContributionDate = DateTime.MinValue,
                ContributionYears = 0
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

    private static async Task<string> ExtractTextAsync(Stream pdfStream)
    {
        await using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var pdf = PdfDocument.Open(memoryStream);

        return string.Join(Environment.NewLine, pdf.GetPages().Select(page => page.Text));
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

    private static bool IsValidNit(string nit)
    {
        if (string.IsNullOrWhiteSpace(nit) || nit.Length != 11 || nit.Any(c => !char.IsDigit(c)))
            return false;

        var weights = new[] { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum = 0;

        for (var i = 0; i < 10; i++)
        {
            sum += (nit[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        var expectedDigit = remainder < 2 ? 0 : 11 - remainder;
        var actualDigit = nit[10] - '0';

        return actualDigit == expectedDigit;
    }
}
