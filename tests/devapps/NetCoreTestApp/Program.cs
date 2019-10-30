// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using NetCoreTestApp.Experimental;
using NetStandard;

namespace NetCoreTestApp
{
    public class Program
    {
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private static readonly string s_username = ""; // used for WIA and U/P, cannot be empty on .net core

        private static readonly IEnumerable<string> s_scopes = new[] {
            "user.read", "openid" }; // used for WIA and U/P, can be empty

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.json";

        private static readonly string[] s_tids = new[]  {
            "common",
            "49f548d0-12b7-4169-a390-bb5304d24462",
            "72f988bf-86f1-41af-91ab-2d7cd011db47" };

        private static int s_currentTid = 0;

        public static void Main(string[] args)
        {
            var pca = CreatePca();
            RunConsoleAppLogicAsync(pca).Wait();
        }

        private static string GetAuthority()
        {
            string tenant = s_tids[s_currentTid];
            return $"https://login.microsoftonline.com/{tenant}";
        }

        private static IPublicClientApplication CreatePca()
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithAuthority(GetAuthority())
                            .WithLogging(Log, LogLevel.Verbose, true)
                            .WithRedirectUri("http://localhost") // required for DefaultOsBrowser
                            .Build();

            pca.UserTokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                    ? File.ReadAllBytes(CacheFilePath)
                    : null);
            });
            pca.UserTokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(CacheFilePath, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
            return pca;
        }

        private static async Task RunConsoleAppLogicAsync(IPublicClientApplication pca)
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
                        4. Acquire Token Interactive (via CustomWebUI)
                        5. Acquire Token Interactive
                        6. Acquire Token Silently
                        7. Acquire Interactive (logic in netstandard, default authority)
                        8. Clear cache
                        9. Rotate Tenant ID
                        0. Exit App
                    Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);

                Task<AuthenticationResult> authTask = null;

                try
                {
                    switch (selection)
                    {
                        case 1: // acquire token
                            authTask = pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).WithUsername(s_username).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 2: // acquire token u/p
                            SecureString password = GetPasswordFromConsole();
                            authTask = pca.AcquireTokenByUsernamePassword(s_scopes, s_username, password).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 3:
                            authTask = pca.AcquireTokenWithDeviceCode(
                                s_scopes,
                                deviceCodeResult =>
                                {
                                    Console.WriteLine(deviceCodeResult.Message);
                                    return Task.FromResult(0);
                                }).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 4: // acquire token interactive with custom web ui

                            authTask = pca.AcquireTokenInteractive(s_scopes)
                                .WithCustomWebUi(new DefaultOsBrowserWebUi()) // make sure you've configured a redirect uri of "http://localhost" or "http://localhost:1234" in the _pca builder
                                .ExecuteAsync(CancellationToken.None);

                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 5: // acquire token interactive

                            var options = new SystemWebViewOptions()
                            {
                                BrowserRedirectSuccess = new Uri("https://www.bing.com?q=why+is+42+the+meaning+of+life")
                            };

                            var cts = new CancellationTokenSource();
                            authTask = pca.AcquireTokenInteractive(s_scopes)
                                .WithSystemWebViewOptions(options)
                                .ExecuteAsync(cts.Token);

                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 6: // acquire token silent
                            IAccount account = pca.GetAccountsAsync().Result.FirstOrDefault();
                            if (account == null)
                            {
                                Log(LogLevel.Error, "Test App Message - no accounts found, AcquireTokenSilentAsync will fail... ", false);
                            }

                            authTask = pca.AcquireTokenSilent(s_scopes, account).ExecuteAsync(CancellationToken.None);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 7:
                            CancellationTokenSource cts2 = new CancellationTokenSource();
                            NetStandardAuthenticator authenticator = new NetStandardAuthenticator(Log, CacheFilePath);
                            await FetchTokenAndCallGraphAsync(pca, authenticator.GetTokenInteractiveAsync(cts2.Token)).ConfigureAwait(false);
                            break;

                        case 8:
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts)
                            {
                                await pca.RemoveAsync(acc).ConfigureAwait(false);
                            }

                            break;

                        case 9:

                            s_currentTid = (s_currentTid + 1) % s_tids.Length;
                            pca = CreatePca();
                            RunConsoleAppLogicAsync(pca).Wait();
                            break;

                        case 0:
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

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            await DisplayAccountsAsync(pca).ConfigureAwait(false);
            var callGraphTask = CallGraphAsync(authTask.Result.AccessToken);
            callGraphTask.Wait();
            Console.WriteLine("Result from calling the ME endpoint of the graph: " + callGraphTask.Result);
            Console.ResetColor();
        }

        private static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count > 0)
                {
                    return certs[0];
                }
                throw new InvalidOperationException($"Cannot find certificate with thumbprint '{thumbprint}'");
            }
        }

        private static async Task DisplayAccountsAsync(IPublicClientApplication pca)
        {
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "For the public client, the tokenCache contains {0} token(s)", accounts.Count()));

            foreach (var account in accounts)
            {
                Console.WriteLine("Account for: " + account.Username + "\n");
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
