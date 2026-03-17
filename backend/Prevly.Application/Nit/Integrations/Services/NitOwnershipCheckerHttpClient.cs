using Microsoft.Extensions.Logging;
using Prevly.Application.Nit.Dtos;
using Prevly.Application.Nit.Integrations.Interfaces;
using Provly.Shared.Captcha;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Prevly.Application.Nit.Integrations.Services;

public sealed class NitOwnershipChecker(
    ILogger<NitOwnershipChecker> logger,
    NitOwnershipCheckerHttpClientConfig? config = null
) : INitOwnershipChecker
{
    private readonly NitOwnershipCheckerHttpClientConfig _config = config ?? NitOwnershipCheckerHttpClientConfig.Default;

    public async Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nit))
            throw new ArgumentException("NIT obrigatorio para consulta de titularidade.");

        if (!long.TryParse(nit, out var nitNumber))
            throw new ArgumentException("NIT invalido para consulta de titularidade.");

        var captchaSolver = new CapMonsterCloudSolver();
        var recaptchaToken = captchaSolver.SolveRecaptchaV2Enterprise(_config.RecaptchaSiteKey, _config.QueryUrl);

        var payload = new
        {
            nit = nitNumber,
            categoria = _config.CategoryValue,
            recaptcha = recaptchaToken
        };

        using var handler = new HttpClientHandler();
        if (!string.IsNullOrWhiteSpace(_config.ProxyUrl))
        {
            handler.Proxy = new WebProxy(_config.ProxyUrl);
            handler.UseProxy = true;
        }

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(_config.TimeoutMs)
        };

        using var request = BuildRequest(payload);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Falha na consulta HTTP de titularidade para NIT {Nit}. Status: {StatusCode}. Body: {Body}",
                nit,
                (int)response.StatusCode,
                responseBody
            );

            throw new HttpRequestException(
                $"Falha ao consultar titularidade do NIT {nit}. Status: {(int)response.StatusCode}."
            );
        }

        var contributor = TryExtractContributor(responseBody);
        if (contributor.HasContributorObject)
        {
            // Regra acordada: se contribuinte.name vier null, considera sem vinculo.
            var belongsToSomeone = !string.IsNullOrWhiteSpace(contributor.OwnerName);
            return new NitOwnershipCheckResultDto(
                BelongsToSomeone: belongsToSomeone,
                OwnerName: contributor.OwnerName
            );
        }

        var content = responseBody.ToLowerInvariant();
        var hasOwnedIndicator = ContainsAny(content, _config.OwnedIndicators);
        var hasNotOwnedIndicator = ContainsAny(content, _config.NotOwnedIndicators);

        if (hasOwnedIndicator && hasNotOwnedIndicator)
            logger.LogWarning("Resultado ambiguo na consulta de titularidade para NIT {Nit}.", nit);

        if (hasOwnedIndicator)
            return new NitOwnershipCheckResultDto(BelongsToSomeone: true, OwnerName: null);

        if (hasNotOwnedIndicator)
            return new NitOwnershipCheckResultDto(BelongsToSomeone: false, OwnerName: null);

        throw new InvalidOperationException("Nao foi possivel determinar o resultado da consulta de titularidade.");
    }

    private HttpRequestMessage BuildRequest(object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _config.ConfirmOptionsUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Host = _config.Host;
        request.Headers.UserAgent.ParseAdd(_config.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        request.Headers.AcceptLanguage.ParseAdd(_config.AcceptLanguage);
        request.Headers.Referrer = new Uri(_config.Referer);
        request.Headers.TryAddWithoutValidation("Origin", _config.Origin);
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
        request.Headers.TryAddWithoutValidation("Priority", "u=0");
        request.Headers.Connection.Clear();
        request.Headers.Connection.Add("keep-alive");

        return request;
    }

    private static bool ContainsAny(string content, IEnumerable<string> indicators)
    {
        foreach (var indicator in indicators)
        {
            if (string.IsNullOrWhiteSpace(indicator))
                continue;

            if (content.Contains(indicator.ToLowerInvariant(), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static (bool HasContributorObject, string? OwnerName) TryExtractContributor(string responseBody)
    {
        try
        {
            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            if (!root.TryGetProperty("contribuinte", out var contributor))
                return (false, null);

            if (contributor.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return (true, null);

            if (contributor.ValueKind != JsonValueKind.Object)
                return (true, null);

            if (!contributor.TryGetProperty("name", out var nameProperty))
                return (true, null);

            if (nameProperty.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return (true, null);

            var ownerName = nameProperty.GetString();
            return (true, string.IsNullOrWhiteSpace(ownerName) ? null : ownerName.Trim());
        }
        catch
        {
            return (false, null);
        }
    }
}

public sealed record NitOwnershipCheckerHttpClientConfig(
    int TimeoutMs,
    string QueryUrl,
    string ConfirmOptionsUrl,
    string RecaptchaSiteKey,
    string CategoryValue,
    string Host,
    string UserAgent,
    string AcceptLanguage,
    string Origin,
    string Referer,
    string? ProxyUrl,
    IReadOnlyCollection<string> OwnedIndicators,
    IReadOnlyCollection<string> NotOwnedIndicators
)
{
    public static NitOwnershipCheckerHttpClientConfig Default { get; } = new(
        TimeoutMs: 30000,
        QueryUrl: "https://sal.rfb.gov.br/calculo-contribuicao/contribuintes-1",
        ConfirmOptionsUrl: "https://sal-spa.dataprev.gov.br/SalSpaApi/api/calculo-contribuicao/confirmar-opcoes-calculo",
        RecaptchaSiteKey: "6Le7YegkAAAAAFNIhuu_eBRaDmxLY6Qf_A8BrtKX",
        CategoryValue: "AUTONOMO_OU_CONTRIBUINTE_INDIVIDUAL",
        Host: "sal-spa.dataprev.gov.br",
        UserAgent: "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:148.0) Gecko/20100101 Firefox/148.0",
        AcceptLanguage: "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7",
        Origin: "https://sal.rfb.gov.br",
        Referer: "https://sal.rfb.gov.br/",
        ProxyUrl: null,
        OwnedIndicators: ["ja cadastrado", "vinculado", "pertence a outra pessoa"],
        NotOwnedIndicators: ["nao localizado", "sem vinculo", "disponivel"]
    );
}
