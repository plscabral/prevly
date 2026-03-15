using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;

namespace Prevly.Application.SocialSecurityRegistration.Integrations.Services;

public sealed class NitOwnershipCheckerPlaywright(
    ILogger<NitOwnershipCheckerPlaywright> logger,
    NitOwnershipCheckerPlaywrightConfig? config = null
) : INitOwnershipChecker
{
    private readonly NitOwnershipCheckerPlaywrightConfig _config = config ?? NitOwnershipCheckerPlaywrightConfig.Default;

    public async Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nit))
            throw new ArgumentException("NIT obrigatorio para consulta de titularidade.");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _config.Headless,
            Args = ["--disable-dev-shm-usage"]
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_config.QueryUrl, new PageGotoOptions
        {
            Timeout = _config.TimeoutMs,
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await SelectCategoryAsync(page);
        await FillNitAsync(page, nit);
        await ClickSubmitAsync(page);

        await page.WaitForTimeoutAsync(1500);

        var visibleText = await page.EvaluateAsync<string>("() => document.body?.innerText ?? ''");
        var content = visibleText.ToLowerInvariant();

        var hasOwnedIndicator = ContainsAny(content, _config.OwnedIndicators);
        var hasNotOwnedIndicator = ContainsAny(content, _config.NotOwnedIndicators);

        if (hasOwnedIndicator && hasNotOwnedIndicator)
            logger.LogWarning("Resultado ambiguo na consulta de titularidade para NIT {Nit}.", nit);

        if (hasOwnedIndicator)
            return new NitOwnershipCheckResultDto(BelongsToSomeone: true);

        if (hasNotOwnedIndicator)
            return new NitOwnershipCheckResultDto(BelongsToSomeone: false);

        throw new InvalidOperationException("Nao foi possivel determinar o resultado da consulta de titularidade.");
    }

    private async Task SelectCategoryAsync(IPage page)
    {
        try
        {
            var categorySelect = page.Locator(_config.CategorySelectSelector).First;
            if (await categorySelect.CountAsync() > 0)
            {
                var options = await categorySelect.Locator("option").AllTextContentsAsync();
                var selected = options.FirstOrDefault(x =>
                    x.Contains("autonom", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(selected))
                {
                    await categorySelect.SelectOptionAsync(new SelectOptionValue
                    {
                        Label = selected
                    });
                    return;
                }

                await categorySelect.SelectOptionAsync(new SelectOptionValue
                {
                    Value = _config.CategoryValue
                });
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Falha ao selecionar categoria por select.");
        }

        // Fallback para componentes customizados sem <select>.
        var categoryTrigger = page.GetByText("Categoria", new PageGetByTextOptions { Exact = false }).First;
        if (await categoryTrigger.CountAsync() == 0)
            throw new InvalidOperationException("Nao foi possivel localizar o campo de categoria.");

        await categoryTrigger.ClickAsync();

        var categoryOption = page.GetByText("Autônomo", new PageGetByTextOptions { Exact = false }).First;
        if (await categoryOption.CountAsync() == 0)
            categoryOption = page.GetByText("Autonomo", new PageGetByTextOptions { Exact = false }).First;

        if (await categoryOption.CountAsync() == 0)
            throw new InvalidOperationException("Nao foi possivel localizar a opcao de categoria Autonomo.");

        await categoryOption.ClickAsync();
    }

    private async Task FillNitAsync(IPage page, string nit)
    {
        var nitInput = page.Locator(_config.NitInputSelector).First;
        if (await nitInput.CountAsync() == 0)
            nitInput = page.GetByLabel("NIT", new PageGetByLabelOptions { Exact = false }).First;

        if (await nitInput.CountAsync() == 0)
            throw new InvalidOperationException("Nao foi possivel localizar o campo de NIT.");

        await nitInput.FillAsync(nit, new LocatorFillOptions { Timeout = _config.TimeoutMs });
    }

    private async Task ClickSubmitAsync(IPage page)
    {
        var submitButton = page.Locator(_config.SubmitButtonSelector).First;
        if (await submitButton.CountAsync() > 0)
        {
            await submitButton.ClickAsync(new LocatorClickOptions { Timeout = _config.TimeoutMs });
            return;
        }

        var buttonByText = page.GetByText("Consultar", new PageGetByTextOptions { Exact = false }).First;
        if (await buttonByText.CountAsync() == 0)
            throw new InvalidOperationException("Nao foi possivel localizar o botao Consultar.");

        await buttonByText.ClickAsync(new LocatorClickOptions { Timeout = _config.TimeoutMs });
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

public sealed record NitOwnershipCheckerPlaywrightConfig(
    bool Headless,
    int TimeoutMs,
    string QueryUrl,
    string CategorySelectSelector,
    string CategoryValue,
    string NitInputSelector,
    string SubmitButtonSelector,
    IReadOnlyCollection<string> OwnedIndicators,
    IReadOnlyCollection<string> NotOwnedIndicators
)
{
    public static NitOwnershipCheckerPlaywrightConfig Default { get; } = new(
        Headless: true,
        TimeoutMs: 30000,
        QueryUrl: "https://sal.rfb.gov.br/calculo-contribuicao/contribuintes-1",
        CategorySelectSelector: "select[name='categoria'], select[formcontrolname='categoria'], select",
        CategoryValue: "AUTONOMO",
        NitInputSelector: "input[name='nit'], input[formcontrolname='nit'], input[placeholder*='NIT' i], input[aria-label*='NIT' i]",
        SubmitButtonSelector: "button:has-text('Consultar'), [type='submit']",
        OwnedIndicators: ["ja cadastrado", "vinculado", "pertence a outra pessoa"],
        NotOwnedIndicators: ["nao localizado", "sem vinculo", "disponivel"]
    );
}
