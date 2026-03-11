using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Prevly.Application.SocialSecurityRegistration.Dtos;
using Prevly.Application.SocialSecurityRegistration.Integrations.Interfaces;

namespace Prevly.Application.SocialSecurityRegistration.Integrations.Services;

public sealed class NitOwnershipCheckerPlaywright(
    ILogger<NitOwnershipCheckerPlaywright> logger
) : INitOwnershipChecker
{
    private const bool Headless = true;
    private const int TimeoutMs = 30000;
    private const string QueryUrl = "https://sal.rfb.gov.br/calculo-contribuicao/contribuintes-1";
    private const string CategorySelectSelector = "select[name='categoria'], select[formcontrolname='categoria'], select";
    private const string CategoryValue = "AUTONOMO";
    private const string NitInputSelector = "input[name='nit'], input[formcontrolname='nit'], input[placeholder*='NIT' i], input[aria-label*='NIT' i]";
    private const string SubmitButtonSelector = "button:has-text('Consultar'), [type='submit']";
    private static readonly string[] OwnedIndicators = ["ja cadastrado", "vinculado", "pertence a outra pessoa"];
    private static readonly string[] NotOwnedIndicators = ["nao localizado", "sem vinculo", "disponivel"];

    public async Task<NitOwnershipCheckResultDto> CheckAsync(string nit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nit))
            throw new ArgumentException("NIT obrigatorio para consulta de titularidade.");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            Args = ["--disable-dev-shm-usage"]
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(QueryUrl, new PageGotoOptions
        {
            Timeout = TimeoutMs,
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await SelectCategoryAsync(page);
        await FillNitAsync(page, nit);
        await ClickSubmitAsync(page);

        await page.WaitForTimeoutAsync(1500);

        var content = (await page.ContentAsync()).ToLowerInvariant();

        var hasOwnedIndicator = ContainsAny(content, OwnedIndicators);
        var hasNotOwnedIndicator = ContainsAny(content, NotOwnedIndicators);

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
            var categorySelect = page.Locator(CategorySelectSelector).First;
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
                    Value = CategoryValue
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
        var nitInput = page.Locator(NitInputSelector).First;
        if (await nitInput.CountAsync() == 0)
            nitInput = page.GetByLabel("NIT", new PageGetByLabelOptions { Exact = false }).First;

        if (await nitInput.CountAsync() == 0)
            throw new InvalidOperationException("Nao foi possivel localizar o campo de NIT.");

        await nitInput.FillAsync(nit, new LocatorFillOptions { Timeout = TimeoutMs });
    }

    private async Task ClickSubmitAsync(IPage page)
    {
        var submitButton = page.Locator(SubmitButtonSelector).First;
        if (await submitButton.CountAsync() > 0)
        {
            await submitButton.ClickAsync(new LocatorClickOptions { Timeout = TimeoutMs });
            return;
        }

        var buttonByText = page.GetByText("Consultar", new PageGetByTextOptions { Exact = false }).First;
        if (await buttonByText.CountAsync() == 0)
            throw new InvalidOperationException("Nao foi possivel localizar o botao Consultar.");

        await buttonByText.ClickAsync(new LocatorClickOptions { Timeout = TimeoutMs });
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
