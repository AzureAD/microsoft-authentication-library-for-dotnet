// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache.Items;
#if NET47
using Microsoft.Identity.Client.Desktop;
#endif
using Microsoft.Identity.Client.SSHCertificates;
using Microsoft.Identity.Client.Utils;

namespace NetFx
{
    public class Program
    {
        private const string Claims = @" {
   ""userinfo"":
    {
     ""given_name"": {""essential"": true},
     ""nickname"": null,
     ""email"": {""essential"": true},
     ""email_verified"": {""essential"": true},
     ""picture"": null,
     ""http://example.info/claims/groups"": null
    },
   ""id_token"":
    {
     ""auth_time"": {""essential"": true},
     ""acr"": {""values"": [""urn:mace:incommon:iap:silver""] }
    },  
  }";
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private const string PoPValidatorEndpoint = "https://signedhttprequest.azurewebsites.net/api/validateSHR";
        private const string PoPUri = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

        private static readonly HttpMethod s_popMethod = HttpMethod.Get;

        private static bool s_usePoP = false;
        private static bool s_useBroker = true;

        // These are not really secret as they do not protect anything, but validaton tools will complain
        // if we have secrets in the code. 

        // Simple confidential client app with access to https://graph.microsoft.com/.default
        private static readonly string s_clientIdForConfidentialApp =
            Environment.GetEnvironmentVariable("LAB_APP_CLIENT_ID");

        // App secret for app above 
        private static readonly string s_confidentialClientSecret =
            Environment.GetEnvironmentVariable("LAB_APP_CLIENT_SECRET");

        // https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourceGroups/ADALTesting/providers/Microsoft.KeyVault/vaults/buildautomation/secrets
        private static readonly string s_secretForPoPValidationRequest =
            Environment.GetEnvironmentVariable("POP_VALIDATIONAPI_SECRET");

        private static readonly string s_username = ""; // used for WIA and U/P, cannot be empty on .net core
        private static readonly IEnumerable<string> s_scopes = new[] { "api://51eb3dd6-d8b5-46f3-991d-b1d4870de7de/myaccess",  };

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        public static readonly string UserCacheFile = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.user.json";
        public static readonly string AppCacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.app.json";

        private static readonly string[] s_authorities = new[]  {
            "https://login.microsoftonline.com/61411618-6f67-4fc5-ba6a-4a0fe32d4eec",
            "https://login.microsoftonline.com/organizations",
            "https://login.microsoftonline.com/49f548d0-12b7-4169-a390-bb5304d24462",
            "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47" };

        private static int s_currentAuthority = 0;

#pragma warning disable UseAsyncSuffix // Use Async suffix
        public static async Task Main(string[] args)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            Console.ResetColor();
            Console.BackgroundColor = ConsoleColor.Black;
            var pca = CreatePca();

            await RunConsoleAppLogicAsync(pca).ConfigureAwait(false);
        }

        private static string GetAuthority()
        {
            return s_authorities[s_currentAuthority];
        }

        private static IConfidentialClientApplication CreateCca()
        {
            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                .Create(s_clientIdForConfidentialApp)
                .WithClientSecret(s_confidentialClientSecret)
                .Build();

            //cca.AcquireTokenOnBehalfOf(null, null).WithAuthority
            

            BindCache(cca.UserTokenCache, UserCacheFile);
            BindCache(cca.AppTokenCache, UserCacheFile);

            return cca;
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        private static IPublicClientApplication CreatePca()
        {
            var builder = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithAuthority(GetAuthority())
#if NET47
                    .WithDesktopFeatures()
#endif
                            .WithLogging(Log, LogLevel.Verbose, true);

            Console.WriteLine($"IsBrokerAvailable: {builder.IsBrokerAvailable()}");

            if (s_useBroker)
            {
                IntPtr consoleWindowHandle = GetConsoleWindow();
                Func<IntPtr> consoleWindowHandleProvider = () => consoleWindowHandle;

                builder = builder
                    //.WithParentActivityOrWindow(consoleWindowHandleProvider)
                    .WithExperimentalFeatures()
                    

                    .WithBroker(true);
            }

            var pca = builder.Build();

            BindCache(pca.UserTokenCache, UserCacheFile);
            return pca;
        }

        private static void BindCache(ITokenCache tokenCache, string file)
        {
            tokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(file)
                    ? File.ReadAllBytes(UserCacheFile)
                    : null);
            });

            tokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(file, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
        }

        private static async Task RunConsoleAppLogicAsync(
            IPublicClientApplication pca)
        {

            while (true)
            {
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"" +
                     $"IsDesktopSession: {pca.IsUserInteractive()}, " +
                     $"IsEmbeddedWebViewAvailable: {pca.IsEmbeddedWebViewAvailable()} " +
                     $"IsEmbeddedWebViewAvailable: {pca.IsSystemWebViewAvailable()}");
                     
                Console.WriteLine("Authority: " + GetAuthority());
                Console.WriteLine("Use WAM: " + s_useBroker);
                await DisplayAccountsAsync(pca).ConfigureAwait(false);

                // display menu
                Console.WriteLine(@$"
                        1. IWA
                        2. Acquire Token with Username and Password
                        3. Acquire Token with Device Code
                        4. Acquire Token Interactive                         
                        $. Acquire Token Interactive with login hint
                        5. Acquire Token Silently
                        6. Acquire Token Silently - multiple requests in parallel
                        7. Acquire SSH Cert Interactive
                        8. Client Credentials 
                        9. Get Account with ID
                        a. Acquire Token Silently with MSA passthrough workaround
                        p. Toggle POP (currently {(s_usePoP ? "ON" : "OFF")}) 
                        b. Toggle broker
                        c. Clear cache
                        r. Rotate Tenant ID
                        e. Expire all ATs
                        x. Exit app
                    Enter your Selection: ");
                char.TryParse(Console.ReadLine(), out var selection);

                try
                {
                    switch (selection)
                    {
                        case '1': // acquire token
                            var iwaBuilder =
                                pca.AcquireTokenByIntegratedWindowsAuth(s_scopes)
                                .WithUsername(s_username);

                            var result = await iwaBuilder.ExecuteAsync().ConfigureAwait(false);

                            await CallApiAsync(pca, result).ConfigureAwait(false);

                            break;
                        case '2': // acquire token u/p
                            Console.WriteLine("Enter username:");
                            string username = Console.ReadLine();
                            SecureString password = GetPasswordFromConsole();
                            var upBuilder = pca.AcquireTokenByUsernamePassword(s_scopes, username, password);

                            result = await upBuilder.ExecuteAsync().ConfigureAwait(false);

                            await CallApiAsync(pca, result).ConfigureAwait(false);

                            break;
                        case '3':
                            var deviceCodeBuilder = pca.AcquireTokenWithDeviceCode(
                                s_scopes,
                                deviceCodeResult =>
                                {
                                    Console.WriteLine(deviceCodeResult.Message);
                                    return Task.FromResult(0);
                                });

                            result = await deviceCodeBuilder.ExecuteAsync().ConfigureAwait(false);
                            await CallApiAsync(pca, result).ConfigureAwait(false);

                            break;
                        case '4':
                            //IntPtr consoleWindowHandle = GetConsoleWindow();
                            var interactiveBuilder = pca
                                .AcquireTokenInteractive(s_scopes);
                                //.WithParentActivityOrWindow(consoleWindowHandle);
                            

                            AuthenticationResult authResult = await interactiveBuilder.ExecuteAsync().ConfigureAwait(false);
                            ClaimsPrincipal idTokenClaims = authResult.ClaimsPrincipal;

                            var accounts2 = await pca.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts2)
                            {
                                Console.WriteLine($"Account for {acc.Username}");
                                foreach (var tp in acc.GetTenantProfiles())
                                {
                                    Console.WriteLine($"Tenant Profile in tenant {tp.TenantId} " +
                                        $"is home tenant? {tp.IsHomeTenant} " +
                                        $"claims {tp.ClaimsPrincipal.Claims.Count()}");
                                }
                            }

                            await CallApiAsync(pca, authResult).ConfigureAwait(false);

                            break;
                        case '$':

                            IAccount account4 = pca.GetAccountsAsync().Result.FirstOrDefault();
                            var interactiveBuilder2 = pca.AcquireTokenInteractive(s_scopes);

                            interactiveBuilder2 = interactiveBuilder2.WithLoginHint(account4.Username);

                            result = await interactiveBuilder2.ExecuteAsync().ConfigureAwait(false);
                            await CallApiAsync(pca, result).ConfigureAwait(false);

                            break;
                        case '5':
                            IAccount account3 = pca.GetAccountsAsync().Result.FirstOrDefault();
                            if (account3 == null)
                            {
                                Log(LogLevel.Error, "Test App Message - no accounts found, AcquireTokenSilentAsync will fail... ", false);
                            }
                            AcquireTokenSilentParameterBuilder silentBuilder2 = pca.AcquireTokenSilent(s_scopes, account3);
                            result = await silentBuilder2.ExecuteAsync().ConfigureAwait(false);
                            await CallApiAsync(pca, result).ConfigureAwait(false);
                            break;

                        case 'a': // acquire token silent with MSA-passthrough
                            IAccount account = pca.GetAccountsAsync().Result.FirstOrDefault();

                            if (account == null)
                            {
                                Log(LogLevel.Error, "Test App Message - no accounts found, AcquireTokenSilentAsync will fail... ", false);
                            }

                            AcquireTokenSilentParameterBuilder silentBuilder = pca.AcquireTokenSilent(s_scopes, account);

                            if (s_usePoP)
                            {
                                var popConfig = new PoPAuthenticationConfiguration(new Uri(PoPUri)) {HttpMethod = s_popMethod };
                                silentBuilder = silentBuilder
                                    .WithExtraQueryParameters(GetTestSliceParams())
                                    .WithProofOfPossession(popConfig);
                            }

                            // this is the same in all clouds
                            const string PersonalTenantIdV2AAD = "9188040d-6c67-4c5b-b112-36a304b66dad";

                            // these are per cloud
                            string publicCloudEnv = "https://login.microsoftonline.com/";
                            string msaTenantIdPublicCloud = "f8cdef31-a31e-4b4a-93e4-5f571e91255a";

                            if (account != null && account.HomeAccountId.TenantId == PersonalTenantIdV2AAD)
                            {
                                var msaAuthority = $"{publicCloudEnv}{msaTenantIdPublicCloud}";

                                silentBuilder = silentBuilder.WithAuthority(msaAuthority);
                            }

                            result = await silentBuilder.ExecuteAsync().ConfigureAwait(false);
                            await CallApiAsync(pca, result).ConfigureAwait(false);

                            break;

                        case '6': // acquire token silent - one request per IAccount
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            Task<AuthenticationResult>[] tasks = accounts
                                .Select(acc =>
                                {
                                    var silentBuilder = pca.AcquireTokenSilent(s_scopes, acc);
                                    if (s_usePoP)
                                    {
                                        var popConfig = new PoPAuthenticationConfiguration(new Uri(PoPUri)) { HttpMethod = s_popMethod };

                                        silentBuilder = silentBuilder
                                            .WithExtraQueryParameters(GetTestSliceParams())
                                            .WithProofOfPossession(popConfig);
                                    }
                                    return silentBuilder.ExecuteAsync();
                                })
                                .ToArray();

                            AuthenticationResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

                            foreach (var ar in results)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine($"Got a token for {ar.Account.Username} ");
                                Console.ResetColor();
                            }

                            break;
                        case '7': // acquire SSH cert
                            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                            RSAParameters rsaKeyInfo = rsa.ExportParameters(false);

                            string modulus = Base64UrlHelpers.Encode(rsaKeyInfo.Modulus);
                            string exp = Base64UrlHelpers.Encode(rsaKeyInfo.Exponent);
                            string jwk = $"{{\"kty\":\"RSA\", \"n\":\"{modulus}\", \"e\":\"{exp}\"}}";

                            CancellationTokenSource cts = new CancellationTokenSource();
                            result = await pca.AcquireTokenInteractive(s_scopes)
                                .WithUseEmbeddedWebView(false)
                                .WithExtraQueryParameters(new Dictionary<string, string>() {
                                    { "dc", "prod-wst-test1"},
                                    { "slice", "test"},
                                    { "sshcrt", "true" }
                                })
                                .WithSSHCertificateAuthenticationScheme(jwk, "1")
                                .WithSystemWebViewOptions(new SystemWebViewOptions()
                                {
                                    HtmlMessageSuccess = "All good, close the browser!",
                                    OpenBrowserAsync = SystemWebViewOptions.OpenWithEdgeBrowserAsync
                                })
                                .ExecuteAsync(cts.Token)
                                .ConfigureAwait(false);

                            Console.WriteLine("SSH cert: " + result.AccessToken);

                            break;
                        case '8':

                            for (int i = 0; i < 100; i++)
                            {
                                var cca = CreateCca();

                                var resultX = await cca.AcquireTokenForClient(
                                    new[] { "https://graph.microsoft.com/.default" })
                                    .WithForceRefresh(true)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);

                                await Task.Delay(500).ConfigureAwait(false);
                                Console.WriteLine("Got a token");
                            }

                            Console.WriteLine("Finished");

                            break;
                        case '9':
                            var accres = await pca.GetAccountAsync("some_id").ConfigureAwait(false);
                            break;
                        case 'b':
                            s_useBroker = !s_useBroker;
                            pca = CreatePca();
                            RunConsoleAppLogicAsync(pca).Wait();

                            break;

                        case 'c':
                            var accounts3 = await pca.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts3)
                            {
                                await pca.RemoveAsync(acc).ConfigureAwait(false);
                            }

                            break;
                        case 'r': // rotate tid

                            s_currentAuthority = (s_currentAuthority + 1) % s_authorities.Length;
                            pca = CreatePca();
                            RunConsoleAppLogicAsync(pca).Wait();
                            break;

                        case 'e': // expire all ATs
                            await (pca.UserTokenCache as TokenCache).ExpireAllAccessTokensForTestAsync().ConfigureAwait(false);
                            break;
                        case 'x':
                            return;
                        default:
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, ex.Message, false);
                    Log(LogLevel.Error, ex.StackTrace, false);
                }

                Console.WriteLine("\n\nHit 'ENTER' to continue...");
                Console.ReadLine();
            }
        }

        //private static T ConfigurePoP<T>(AbstractPublicClientAcquireTokenParameterBuilder<T> builder)
        //    where T : AbstractPublicClientAcquireTokenParameterBuilder<T>
        //{
        //    if (s_usePoP)
        //    {
        //        var popConfig = new PopAuthenticationConfiguration(new Uri(PoPUri)) { HttpMethod = s_popMethod };
        //        builder = builder
        //            .WithExtraQueryParameters(GetTestSliceParams())
        //            .WithProofOfPosession(popConfig);
        //    }

        //    return builder as T;
        //}

        private static async Task CallApiAsync(IPublicClientApplication pca, AuthenticationResult authResult)
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Token is {0}", authResult.AccessToken);
            Console.ResetColor();

            string authHeader = authResult.CreateAuthorizationHeader();

            if (s_usePoP)
            {
                await CallPoPVerificationAPIAsync(authHeader).ConfigureAwait(false);
            }
            else
            {
                await CallGraphAsync(authHeader).ConfigureAwait(false);
            }

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            await DisplayAccountsAsync(pca).ConfigureAwait(false);
            Console.ResetColor();

        }

        private static async Task CallGraphAsync(string authHeader)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var request = new HttpRequestMessage(HttpMethod.Get, GraphAPIEndpoint);
            request.Headers.Add("Authorization", authHeader);
            response = await httpClient.SendAsync(request).ConfigureAwait(false);

            await PrintHttpResponseAsync(response).ConfigureAwait(false);

        }

        private static async Task PrintHttpResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(content);

                Console.ResetColor();
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(content);

                Console.ResetColor();
            }

        }

        /// <summary>
        /// This calls a special endpoint that validates any POP token against a configurable HTTP request.
        /// The HTTP request is configured through headers.
        /// </summary>
        private static async Task CallPoPVerificationAPIAsync(string authHeader)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var request = new HttpRequestMessage(HttpMethod.Post, PoPValidatorEndpoint);

            request.Headers.Add("Authorization", authHeader);
            request.Headers.Add("Secret", s_secretForPoPValidationRequest);
            request.Headers.Add("Authority", "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/");
            request.Headers.Add("ClientId", s_clientIdForPublicApp);

            // the URI the POP token is bound to
            request.Headers.Add("ShrUri", PoPUri);

            // the method the POP token in bound to
            request.Headers.Add("ShrMethod", s_popMethod.ToString());

            response = await httpClient.SendAsync(request).ConfigureAwait(false);
            await PrintHttpResponseAsync(response).ConfigureAwait(false);
        }

        private static async Task DisplayAccountsAsync(IPublicClientApplication pca)
        {
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "For the public client, the tokenCache contains {0} token(s)", accounts.Count()));

            foreach (var account in accounts)
            {
                Console.WriteLine("_pca account for: " + account.Username + "\n");
            }
        }

        private static void Log(LogLevel level, string message, bool containsPii)
        {
            if (!containsPii)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
            }

            switch (level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    break;
            }

            Console.WriteLine($"{level} {message}");
            Console.ResetColor();
        }

        private static SecureString GetPasswordFromConsole()
        {
            Console.Write("Password: ");
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd;
        }

        private static Dictionary<string, string> GetTestSliceParams()
        {
            return new Dictionary<string, string>()
            {
                { "dc", "prod-wst-test1" },
            };
        }
    }
}
