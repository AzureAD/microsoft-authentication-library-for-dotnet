// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using NetCoreTestApp.Experimental;

namespace NetCoreTestApp
{
    public class Program
    {
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private static readonly string s_username = ""; // used for WIA and U/P, cannot be empty on .net core

        // Confidential client app with access to https://graph.microsoft.com/.default
        private static string s_clientIdForConfidentialApp;

        // App secret for app above 
        private static string s_confidentialClientSecret;

        private static string s_ccaAuthority;

        private static readonly IEnumerable<string> s_scopes = new[] {
            "user.read", "openid" }; // used for WIA and U/P, can be empty

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        private static readonly string[] GraphAppScope = new[] { "https://graph.microsoft.com/.default" };

        public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.json";

        private static readonly string[] s_tids = new[]  {
            "common",
            "organizations",
            "49f548d0-12b7-4169-a390-bb5304d24462",
            "72f988bf-86f1-41af-91ab-2d7cd011db47" };

        private static int s_currentTid = 0;

        public static void Main(string[] args)
        {
            var ccaSettings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            s_clientIdForConfidentialApp = ccaSettings.ClientId;
            s_ccaAuthority = ccaSettings.Authority;
            s_confidentialClientSecret = ccaSettings.GetSecret();

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
            var pcaBuilder = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithAuthority(GetAuthority())
                            .WithLogging(Log, LogLevel.Verbose, true)
                            .WithExperimentalFeatures()
                            .WithDesktopFeatures();

            Console.WriteLine($"IsBrokerAvailable: {pcaBuilder.IsBrokerAvailable()}");

            var pca = pcaBuilder.WithRedirectUri("http://localhost") // required for DefaultOsBrowser
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
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"" +
                    $"IsDesktopSession: {pca.IsUserInteractive()}, " +
                    $"IsEmbeddedWebViewAvailable: {pca.IsEmbeddedWebViewAvailable()} " +
                    $"IsEmbeddedWebViewAvailable: {pca.IsSystemWebViewAvailable()}");

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
                        7. Confidential Client
                        8. Clear cache
                        9. Rotate Tenant ID
                       10. Acquire Token Interactive with Chrome
                       11. AcquireTokenForClient with multiple threads
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
                                //BrowserRedirectSuccess = new Uri("https://www.bing.com?q=why+is+42+the+meaning+of+life")
                                OpenBrowserAsync = SystemWebViewOptions.OpenWithEdgeBrowserAsync
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
                            for (int i = 0; i < 100; i++)
                            {
                                var cca = CreateCca();

                                var resultX = await cca.AcquireTokenForClient(GraphAppScope)
                                    //.WithForceRefresh(true)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);

                                await Task.Delay(500).ConfigureAwait(false);
                                Console.WriteLine("Got a token");
                            }

                            Console.WriteLine("Finished");
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

                        case 10: // acquire token interactive with Chrome

                            var optionsChrome = new SystemWebViewOptions()
                            {
                                //BrowserRedirectSuccess = new Uri("https://www.bing.com?q=why+is+42+the+meaning+of+life")
                                OpenBrowserAsync = SystemWebViewOptions.OpenWithChromeEdgeBrowserAsync
                            };

                            var ctsChrome = new CancellationTokenSource();
                            authTask = pca.AcquireTokenInteractive(s_scopes)
                                .WithSystemWebViewOptions(optionsChrome)
                                .ExecuteAsync(ctsChrome.Token);

                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 11: // AcquireTokenForClient with multiple threads
                            Console.Write("Enter number of threads to start (default 10): ");
                            int totalThreads = int.TryParse(Console.ReadLine(), out totalThreads) ? totalThreads : 10;
                            Console.Write("Enter run duration in seconds (default 10): ");
                            int durationInSeconds = int.TryParse(Console.ReadLine(), out durationInSeconds) ? durationInSeconds : 10;

                            var acquireTokenBuilder = CreateCca().AcquireTokenForClient(GraphAppScope);

                            var threads = new List<Thread>();
                            for (int i = 0; i < totalThreads; i++)
                            {
                                var thread = new Thread(new ThreadStart(new ThreadWork(i, acquireTokenBuilder, durationInSeconds).Run));
                                thread.Name = $"Thread #{i}";
                                threads.Add(thread);
                            }

                            foreach (var thread in threads)
                            {
                                thread.Start();
                            }

                            foreach (var thread in threads)
                            {
                                thread.Join();
                            }

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

        private static IConfidentialClientApplication CreateCca()
        {
            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                .Create(s_clientIdForConfidentialApp)
                .WithAuthority(s_ccaAuthority)
                .WithClientSecret(s_confidentialClientSecret)
                .Build();

            //cca.AppTokenCache.SetBeforeAccess((t) => { });

            //cca.AcquireTokenForClient(new[] "12345-123321-1111/default");

            return cca;
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

        public class ThreadWork
        {
            private readonly int Id;
            private readonly AcquireTokenForClientParameterBuilder AcquireTokenBuilder;
            private readonly DateTimeOffset EndTime;

            public ThreadWork(int id, AcquireTokenForClientParameterBuilder acquireTokenBuilder, int durationInSeconds)
            {
                Id = id;
                AcquireTokenBuilder = acquireTokenBuilder;
                EndTime = DateTimeOffset.UtcNow.AddSeconds(durationInSeconds);
            }

            public void Run()
            {
                Console.WriteLine($"Thread #{Id} start.");
                while (DateTimeOffset.UtcNow < EndTime)
                {
                    try
                    {
                        Task<AuthenticationResult> authenticationResultTask = Task.Run(() =>
                            AcquireTokenBuilder
                                .WithAuthority(s_ccaAuthority, true)
                                .ExecuteAsync());

                        authenticationResultTask.Wait();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Thread #{Id}: {ex}");
                    }
                }
                Console.WriteLine($"Thread #{Id} stop.");

            }
        }
    }
}
