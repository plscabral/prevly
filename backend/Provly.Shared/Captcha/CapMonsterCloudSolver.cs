using Zennolab.CapMonsterCloud;
using Zennolab.CapMonsterCloud.Requests;
using Zennolab.CapMonsterCloud.Responses;

namespace Provly.Shared.Captcha;

public class CapMonsterCloudSolver
    {
        public string ClientKey { get; set; } = "ca370fb4a01f823225f2dce2a40e6ac5";
        
        public int ProxyPort { get; set; } = 50100;
        public string ProxyType { get; set; } = "http";
        public string ProxyLogin { get; set; } = "sipeimpacta";
        public string ProxyPassword { get; set; } = "dU6T6sYm2d";
        public string ProxyAddress { get; set; } = "200.239.217.101";
        
        public string SolveHCaptcha(
            string siteKey,
            string url, 
            bool invisible = false, 
            bool recebeUa = false, 
            bool useProxy = false, 
            IDictionary<string, string>? cookies = null
        )
        {
            var clientOptions = new ClientOptions
            {
                ClientKey = ClientKey
            };

            string? code;

            var cmCloudClient = CapMonsterCloudClientFactory.Create(clientOptions);

            if (useProxy)
            {
                var hcaptchaRequest = new HCaptchaRequest
                {
                    WebsiteUrl = url,
                    WebsiteKey = siteKey,
                    Invisible = invisible,
                    FallbackToActualUA = recebeUa,
                    Cookies = cookies,
                    // ProxyAddress = ProxyAddress,
                    // ProxyPort = ProxyPort,
                    // ProxyLogin = ProxyLogin,
                    // ProxyPassword = ProxyPassword
                };

                var hcaptchaResult = cmCloudClient.SolveAsync(hcaptchaRequest).Result;

                code = hcaptchaResult.Solution != null ? hcaptchaResult.Solution.Value : "";
            }
            else
            {
                var hcaptchaRequest = new HCaptchaRequest
                {
                    WebsiteUrl = url,
                    WebsiteKey = siteKey,
                    Invisible = invisible,
                    FallbackToActualUA = recebeUa,
                    Cookies = cookies
                };

                var hcaptchaResult = cmCloudClient.SolveAsync(hcaptchaRequest).Result;

                code = hcaptchaResult.Solution != null ? hcaptchaResult.Solution.Value : "";
            }

            return code;
        }
        
        public string SolveRecaptchaV2Enterprise(
            string siteKey, 
            string url, 
            bool useProxy = true, 
            string userAgent = ""
        )
        {
            var clientOptions = new ClientOptions
            {
                ClientKey = ClientKey
            };

            var cmCloudClient = CapMonsterCloudClientFactory.Create(clientOptions);

            if (useProxy)
            {
                var hcaptchaRequest = new RecaptchaV2EnterpriseRequest
                {
                    WebsiteUrl = url,
                    WebsiteKey = siteKey
                };

                var captchaResult = cmCloudClient.SolveAsync(hcaptchaRequest).Result;

                return captchaResult.Solution.Value;
            }
            else
            {
                var hcaptchaRequest = new RecaptchaV2EnterpriseRequest
                {
                    WebsiteUrl = url,
                    WebsiteKey = siteKey,
                };


                var captchaResult = cmCloudClient.SolveAsync(hcaptchaRequest).Result;

                return captchaResult.Solution.Value;
            }
        }

        public dynamic SolveTencent(string siteKey, string url, bool useProxy = true, string userAgent = "")
        {
            var clientOptions = new ClientOptions
            {
                ClientKey = ClientKey
            };

            var cmCloudClient = CapMonsterCloudClientFactory.Create(clientOptions);

            if (useProxy)
            {
                var hcaptchaRequest = new TenDiCustomTaskRequest
                {
                    WebsiteUrl = url,
                    WebsiteKey = siteKey,
                    UserAgent = userAgent,
                    // ProxyAddress = ProxyAddress,
                    // ProxyPort = ProxyPort,
                    // ProxyLogin = ProxyLogin,
                    // ProxyPassword = ProxyPassword,
                };

                CaptchaResult<CustomTaskResponse> captchaResult = cmCloudClient.SolveAsync(hcaptchaRequest).Result;

                return new
                {
                    ticket = captchaResult.Solution.Data.FirstOrDefault(x => x.Key == "ticket").Value,
                    randstr = captchaResult.Solution.Data.FirstOrDefault(x => x.Key == "randstr").Value,
                };
            }
            else
            {
                var hcaptchaRequest = new TenDiCustomTaskRequest
                {
                    WebsiteUrl = url,
                    WebsiteKey = siteKey,
                    UserAgent = userAgent,
                };
                
                CaptchaResult<CustomTaskResponse> captchaResult = cmCloudClient.SolveAsync(hcaptchaRequest).Result;

                return new
                {
                    ticket = captchaResult.Solution.Data.FirstOrDefault(x => x.Key == "").Value,
                    randstr = captchaResult.Solution.Data.FirstOrDefault(x => x.Key == "").Value,
                };
            }
        }
        
        public dynamic SolveTurnstile(string siteKey, string url)
        {
            var clientOptions = new Zennolab.CapMonsterCloud.ClientOptions
            {
                ClientKey = ClientKey
            };

            var cmCloudClient = CapMonsterCloudClientFactory.Create(clientOptions);

            var turnstileRequest = new TurnstileRequest
            {
                WebsiteUrl = url,
                WebsiteKey = siteKey,
            };

            CaptchaResult<TurnstileResponse> captchaResult = cmCloudClient.SolveAsync(turnstileRequest).Result;

            return new
            {
                ticket = captchaResult.Solution.Value
            };
        }
    }