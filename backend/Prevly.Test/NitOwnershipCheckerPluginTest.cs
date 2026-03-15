using Microsoft.Playwright;
using NUnit.Framework;

namespace Prevly.Test;

[TestFixture]
public class NitOwnershipCheckerPluginTest
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;

    [SetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();

        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 300
        });

        var context = await _browser.NewContextAsync();

        _page = await context.NewPageAsync();
    }

    [Test]
    public async Task Should_Open_Site()
    {
        await _page.GotoAsync("https://sal.rfb.gov.br/calculo-contribuicao/contribuintes-1");

        // espera a página carregar
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var html = await _page.ContentAsync();
        
        await _page.ClickAsync("label[for='categoria_op_AUTONOMO_OU_CONTRIBUINTE_INDIVIDUAL']");
        
        await _page.Locator("input[formcontrolname='nit']").FillAsync("10950081652");
        
        await _page.ClickAsync("button[type='button']");

        // espera algum resultado carregar
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}