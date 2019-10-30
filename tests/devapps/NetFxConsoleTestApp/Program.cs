// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.SSHCertificates;
using Microsoft.Identity.Client.Utils;
using NetStandard;

namespace NetFx
{
    public class Program
    {
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
        private static readonly string s_clientIdForConfidentialApp =
            Environment.GetEnvironmentVariable("LAB_APP_CLIENT_ID") ??
            throw new ArgumentException("Please configure a client id");

        private static readonly string s_confidentialClientSecret =
            Environment.GetEnvironmentVariable("LAB_APP_CLIENT_SECRET") ??
            throw new ArgumentException("Please configure a client secret");

        private static readonly string s_username = ""; // used for WIA and U/P, cannot be empty on .net core
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read", "Openid", "profile" }; // used for WIA and U/P, can be empty

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        public static readonly string UserCacheFile = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.user.json";
        public static readonly string AppCacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.app.json";


        private static readonly string[] s_tids = new[]  {
            "common",
            "49f548d0-12b7-4169-a390-bb5304d24462",
            "72f988bf-86f1-41af-91ab-2d7cd011db47" };

        private static int s_currentTid = 0;

        public static void Main(string[] args)
        {
            Console.ResetColor();
            Console.BackgroundColor = ConsoleColor.Black;
            var pca = CreatePca();
            var cca = CreateCca();
            RunConsoleAppLogicAsync(pca, cca).Wait();
        }

        private static string GetAuthority()
        {
            string tenant = s_tids[s_currentTid];
            return $"https://login.microsoftonline.com/{tenant}";
        }

        private static IConfidentialClientApplication CreateCca()
        {
            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                .Create(s_clientIdForConfidentialApp)
                .WithClientSecret(s_confidentialClientSecret)
                .Build();

            BindCache(cca.UserTokenCache, UserCacheFile);
            //BindCache(cca.AppTokenCache, AppCacheFilePath);

            return cca;
        }
        private static IPublicClientApplication CreatePca()
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithAuthority(GetAuthority())
                            .WithLogging(Log, LogLevel.Verbose, true)
                            .WithRedirectUri("http://localhost") // required for DefaultOsBrowser
                            .Build();

            BindCache(pca.UserTokenCache, UserCacheFile);
            return pca;
        }

        private static void BindCache(ITokenCache tokenCache, string file)
        {
            tokenCache.SetBeforeAccess(notificationArgs =>
            {
                //Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"SetBeforeAccess invoked for {notificationArgs?.Account?.Username ?? "null"} ");
                Debug.WriteLine($"SetBeforeAccess invoked for {notificationArgs?.Account?.Username ?? "null"} ");
                Console.ResetColor();

                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(file)
                    ? File.ReadAllBytes(UserCacheFile)
                    : null);
            });

            tokenCache.SetAfterAccess(notificationArgs =>
            {
                //Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"SetAfterAccess invoked for {notificationArgs?.Account?.Username ?? "null" } " +
                    $"with HasStateChanges = {notificationArgs.HasStateChanged}");
                Debug.WriteLine($"SetAfterAccess invoked for {notificationArgs?.Account?.Username ?? "null" } " +
                    $"with HasStateChanges = {notificationArgs.HasStateChanged}");
                Console.ResetColor();

                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(file, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
        }

        private static async Task RunConsoleAppLogicAsync(
            IPublicClientApplication pca, 
            IConfidentialClientApplication cca)
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("Authority: " + GetAuthority());
                await DisplayAccountsAsync(pca).ConfigureAwait(false);

                // display menu
                Console.WriteLine(@"
                        1. IWA
                        2. Acquire Token with Username and Password
                        3. Acquire Token with Device Code
                        4. Acquire Token Interactive 
                        5. Acquire Token Interactive via NetStandard lib
                        6. Acquire Token Silently
                        7. Acquire Token Silently - multiple requests in parallel
                        8. Acquire SSH Cert Interactive
                        9. Client Credentials 
                        c. Clear cache
                        r. Rotate Tenant ID
                        e. Expire all ATs
                        x. Exit app
                    Enter your Selection: ");
                char.TryParse(Console.ReadLine(), out var selection);

                Task<AuthenticationResult> authTask = null;

                try
                {
                    switch (selection)
                    {
                        case '1': // acquire token
                            authTask = pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).WithUsername(s_username).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case '2': // acquire token u/p
                            SecureString password = GetPasswordFromConsole();
                            authTask = pca.AcquireTokenByUsernamePassword(s_scopes, s_username, password).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case '3':
                            authTask = pca.AcquireTokenWithDeviceCode(
                                s_scopes,
                                deviceCodeResult =>
                                {
                                    Console.WriteLine(deviceCodeResult.Message);
                                    return Task.FromResult(0);
                                }).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case '4':

                            authTask = pca.AcquireTokenInteractive(s_scopes)
                                .WithPrompt(Prompt.Consent)
                                .ExecuteAsync(CancellationToken.None);

                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);
                            break;

                        case '6': // acquire token silent
                            IAccount account = pca.GetAccountsAsync().Result.FirstOrDefault();
                            if (account == null)
                            {
                                Log(LogLevel.Error, "Test App Message - no accounts found, AcquireTokenSilentAsync will fail... ", false);
                            }

                            authTask = pca.AcquireTokenSilent(s_scopes, account).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case '7': // acquire token silent - one request per IAccount
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            Task<AuthenticationResult>[] tasks = accounts
                                .Select(acc => pca.AcquireTokenSilent(s_scopes, acc).ExecuteAsync())
                                .ToArray();

                            AuthenticationResult[] result = await Task.WhenAll(tasks).ConfigureAwait(false);

                            foreach (var ar in result)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine($"Got a token for {ar.Account.Username} ");
                                Console.ResetColor();
                            }

                            break;
                        case '5': // Acquire Token Interactive via NetStandard lib
                            CancellationTokenSource cts2 = new CancellationTokenSource();
                            var authenticator = new NetStandardAuthenticator(Log, UserCacheFile);
                            await FetchTokenAndCallGraphAsync(pca, authenticator.GetTokenInteractiveAsync(cts2.Token)).ConfigureAwait(false);
                            break;
                        case '8': // acquire SSH cert
                            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                            RSAParameters rsaKeyInfo = rsa.ExportParameters(false);

                            string modulus = Base64UrlHelpers.Encode(rsaKeyInfo.Modulus);
                            string exp = Base64UrlHelpers.Encode(rsaKeyInfo.Exponent);
                            string jwk = $"{{\"kty\":\"RSA\", \"n\":\"{modulus}\", \"e\":\"{exp}\"}}";

                            CancellationTokenSource cts = new CancellationTokenSource();
                            authTask = pca.AcquireTokenInteractive(s_scopes)
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
                                .ExecuteAsync(cts.Token);

                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case '9':

                            authTask = cca.AcquireTokenForClient(
                                new[] { "https://graph.microsoft.com/.default" }).
                                ExecuteAsync();

                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);
                            break;

                        case 'c':
                            var accounts2 = await pca.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts2)
                            {
                                await pca.RemoveAsync(acc).ConfigureAwait(false);
                            }

                            break;
                        case 'r': // rotate tid

                            s_currentTid = (s_currentTid + 1) % s_tids.Length;
                            pca = CreatePca();
                            cca = CreateCca();
                            RunConsoleAppLogicAsync(pca, cca).Wait();
                            break;

                        case 'e': // expire all ATs

                            var tokenCacheInternal = pca.UserTokenCache as ITokenCacheInternal;
                            var ats = tokenCacheInternal.Accessor.GetAllAccessTokens();
                            // set access tokens as expired
                            foreach (var accessItem in ats)
                            {
                                accessItem.ExpiresOnUnixTimestamp =
                                    ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)
                                    .ToString(CultureInfo.InvariantCulture);

                                tokenCacheInternal.Accessor.SaveAccessToken(accessItem);
                            }

                            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs(
                                pca.UserTokenCache as ITokenCacheInternal, s_clientIdForPublicApp, null, true, false);

                            await tokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);

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

        private static async Task FetchTokenAndCallGraphAsync(IPublicClientApplication pca, Task<AuthenticationResult> authTask)
        {
            await authTask.ConfigureAwait(false);

            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Token is {0}", authTask.Result.AccessToken);
            Console.ResetColor();


            await CallGraphAsync(authTask.Result.AccessToken).ConfigureAwait(false);

            await CallGraphAsync(authTask.Result.AccessToken).ConfigureAwait(false);

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            await DisplayAccountsAsync(pca).ConfigureAwait(false);
            Console.ResetColor();


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

        private static async Task<string> CallGraphAsync(string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, GraphAPIEndpoint);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
