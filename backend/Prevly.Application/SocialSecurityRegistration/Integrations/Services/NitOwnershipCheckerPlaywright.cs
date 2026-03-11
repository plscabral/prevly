using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;
using Prevly.Application.SocialSecurityRegistration.Integrations.Settings;

namespace Prevly.Application.SocialSecurityRegistration.Integrations.Services;

public sealed class NitOwnershipCheckerPlaywright(
    IOptions<NitOwnershipCheckerSettings> options,
    ILogger<NitOwnershipCheckerPlaywright> logger
) : INitOwnershipChecker
{
    private readonly NitOwnershipCheckerSettings _settings = options.Value;

    public async Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nit))
            throw new ArgumentException("NIT obrigatorio para consulta de titularidade.");

        if (string.IsNullOrWhiteSpace(_settings.QueryUrl))
            throw new InvalidOperationException("NitOwnershipChecker:QueryUrl nao foi configurado.");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _settings.Headless,
            Args = ["--disable-dev-shm-usage"]
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_settings.QueryUrl, new PageGotoOptions
        {
            Timeout = _settings.TimeoutMs,
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.FillAsync(_settings.NitInputSelector, nit, new PageFillOptions
        {
            Timeout = _settings.TimeoutMs
        });

        await page.ClickAsync(_settings.SubmitButtonSelector, new PageClickOptions
        {
            Timeout = _settings.TimeoutMs
        });

        if (!string.IsNullOrWhiteSpace(_settings.ResultReadySelector))
        {
            await page.WaitForSelectorAsync(_settings.ResultReadySelector, new PageWaitForSelectorOptions
            {
                Timeout = _settings.TimeoutMs
            });
        }
        else
        {
            await page.WaitForTimeoutAsync(1500);
        }

        var content = (await page.ContentAsync()).ToLowerInvariant();

        var hasOwnedIndicator = ContainsAny(content, _settings.OwnedIndicators);
        var hasNotOwnedIndicator = ContainsAny(content, _settings.NotOwnedIndicators);

        if (hasOwnedIndicator && hasNotOwnedIndicator)
            logger.LogWarning("Resultado ambiguo na consulta de titularidade para NIT {Nit}.", nit);

        if (hasOwnedIndicator)
            return new NitOwnershipCheckResultDto(BelongsToSomeone: true);

        if (hasNotOwnedIndicator)
            return new NitOwnershipCheckResultDto(BelongsToSomeone: false);

        throw new InvalidOperationException("Nao foi possivel determinar o resultado da consulta de titularidade.");
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
}
