using Provly.Shared.Captcha;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Prevly.Test;

[TestFixture]
public class NitOwnershipCheckerPluginTest
{
    [Test]
    public async Task Should_Post_Confirmar_Opcoes_Calculo()
    {
        const string url = "https://sal.rfb.gov.br/calculo-contribuicao/contribuintes-1";
        const string siteKey = "6Le7YegkAAAAAFNIhuu_eBRaDmxLY6Qf_A8BrtKX";
        const string endpoint = "https://sal-spa.dataprev.gov.br/SalSpaApi/api/calculo-contribuicao/confirmar-opcoes-calculo";

        var capMonsterCloud = new CapMonsterCloudSolver();
        var recaptchaToken = capMonsterCloud.SolveRecaptchaV2Enterprise(siteKey, url);

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

        #region Headers
        
        request.Headers.Host = "sal-spa.dataprev.gov.br";
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:148.0) Gecko/20100101 Firefox/148.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        request.Headers.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Referrer = new Uri("https://sal.rfb.gov.br/");
        request.Headers.TryAddWithoutValidation("Origin", "https://sal.rfb.gov.br");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
        request.Headers.TryAddWithoutValidation("Priority", "u=0");
        request.Headers.Connection.Clear();
        request.Headers.Connection.Add("keep-alive");

        #endregion
        
        var content =  JsonSerializer.Serialize(new
        {
            nit = 10950081652,
            categoria = "AUTONOMO_OU_CONTRIBUINTE_INDIVIDUAL",
            recaptcha = recaptchaToken
        });
        
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        TestContext.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
        TestContext.WriteLine(responseBody);
        
        Assert.That(response.IsSuccessStatusCode, Is.True, responseBody);
    }
}
