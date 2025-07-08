﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using NetCoreTestApp.Experimental;

namespace NetCoreTestApp
{
    public class Program
    {
        internal /* for test */ static Dictionary<string, string> CallerSDKDetails { get; } = new()
          {
              { "caller-sdk-id", "IdWeb_1" },
              { "caller-sdk-ver", "123" }
          };

        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private static readonly string s_username = ""; // used for WIA and U/P, cannot be empty on .net core

        // Confidential client app with access to https://graph.microsoft.com/.default
        private static string s_clientIdForConfidentialApp;

        // App certificate for app above 
        private static X509Certificate2 s_confidentialClientCertificate;

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

        private static string s_scope = "https://management.azure.com";

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        public static void Main(string[] args)
        {
            var ccaSettings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            s_clientIdForConfidentialApp = ccaSettings.ClientId;
            s_ccaAuthority = ccaSettings.Authority;
            s_confidentialClientCertificate = ccaSettings.GetCertificate();

            var pca = CreatePca();
            RunConsoleAppLogicAsync(pca).Wait();
        }

        private static string GetAuthority()
        {
            string tenant = s_tids[s_currentTid];
            return $"https://login.microsoftonline.com/{tenant}";
        }

        private static IPublicClientApplication CreatePca(bool withWamBroker = false)
        {
            // <PCABootstrapSample>
            var pcaBuilder = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithAuthority(GetAuthority())
                            .WithLogging(Log, LogLevel.Verbose, true);

            if (withWamBroker)
            {
                IntPtr consoleWindowHandle = GetConsoleWindow();
                Func<IntPtr> consoleWindowHandleProvider = () => consoleWindowHandle;
                pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows) { Title = "Only Windows" })
                          .WithParentActivityOrWindow(consoleWindowHandleProvider);
            }

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
            // </PCABootstrapSample>
            return pca;
        }

        private static IManagedIdentityApplication CreateMia()
        {
            IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned)
                            .Build();

            return mia;
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
                       12. Acquire Token Interactive with Broker
                       13. Acquire Token using Managed Identity (VM)
                       14. Acquire Token using Managed Identity (VM) - multiple requests in parallel
                       15. Acquire Confidential Client Token over MTLS SNI + MTLS
                        0. Exit App
                    Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);

                Task<AuthenticationResult> authTask = null;

                try
                {
                    switch (selection)
                    {
                        case 1: // acquire token
#pragma warning disable CS0618 // Type or member is obsolete
                            authTask = pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).WithUsername(s_username).ExecuteAsync(CancellationToken.None);
#pragma warning restore CS0618 // Type or member is obsolete
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;

                        case 2: // acquire token u/p
                            string password = GetPasswordFromConsole();
#pragma warning disable CS0618 // Type or member is obsolete
                            authTask = pca.AcquireTokenByUsernamePassword(s_scopes, s_username, password).ExecuteAsync(CancellationToken.None);
#pragma warning restore CS0618
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
                            var cca = CreateCca();

                            for (int i = 0; i < 1000; i++)
                            {

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
                            int totalThreads = int.TryParse(Console.ReadLine(), out totalThreads) ? totalThreads : 100;
                            Console.Write("Enter run duration in seconds (default 10): ");
                            int durationInSeconds = int.TryParse(Console.ReadLine(), out durationInSeconds) ? durationInSeconds : 10;
                            var cca2 = CreateCca();
                            var acquireTokenBuilder = cca2.AcquireTokenForClient(GraphAppScope)
                                .WithExtraQueryParameters(CallerSDKDetails);

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

                        case 12: // acquire token interactive with WamBroker
                            {
                                var optionsbroker = new SystemWebViewOptions()
                                {
                                    OpenBrowserAsync = SystemWebViewOptions.OpenWithEdgeBrowserAsync
                                };

                                var pcaBroker = CreatePca(true);

                                var ctsBroker = new CancellationTokenSource();
                                authTask = pcaBroker.AcquireTokenInteractive(s_scopes)
                                    .WithSystemWebViewOptions(optionsbroker)
                                    .ExecuteAsync(ctsBroker.Token);

                                await FetchTokenAndCallGraphAsync(pcaBroker, authTask).ConfigureAwait(false);
                            }
                            break;

                        case 13: // managed identity on a vm

                            IManagedIdentityApplication mia1 = CreateMia();

                            AuthenticationResult authenticationResult1 = await mia1.AcquireTokenForManagedIdentity(s_scope)
                                .ExecuteAsync()
                                .ConfigureAwait(false);

                            Console.WriteLine($"Managed Identity token - {authenticationResult1.AccessToken}");

                            break;

                        case 14: // managed identity on a vm - multi threaded

                            IManagedIdentityApplication mia2 = CreateMia();
                            int identityProviderHits = 0;
                            int cacheHits = 0;

                            Task[] miTasks = new Task[10];
                            for (int i = 0; i < 10; i++)
                            {
                                miTasks[i] = Task.Run(async () =>
                                {
                                    AuthenticationResult authResult = await mia2.AcquireTokenForManagedIdentity(s_scope)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);

                                    if (authResult.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider)
                                    {
                                        // Increment identity hits count
                                        Interlocked.Increment(ref identityProviderHits);
                                    }
                                    else
                                    {
                                        // Increment cache hits count
                                        Interlocked.Increment(ref cacheHits);
                                    }
                                });
                            }

                            await Task.WhenAll(miTasks).ConfigureAwait(false);

                            Console.WriteLine($"identity Provider Hits (must be 1 always) - {identityProviderHits}");
                            Console.WriteLine($"cache Hits - {cacheHits}");

                            break;

                        case 15: //acquire token with cert over MTLS SNI + MTLS 

                            var cca1 = CreateCcaForMtlsPop("westus3");

                            var resultX1 = await cca1.AcquireTokenForClient(GraphAppScope)
                                .WithMtlsProofOfPossession()
                                .WithExtraQueryParameters("dc=ESTSR-PUB-WUS3-AZ1-TEST1&slice=TestSlice") //Feature in test slice
                                .ExecuteAsync()
                                .ConfigureAwait(false);

                            Console.WriteLine("Got a token");
                            Console.WriteLine("Finished");
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
            ConfidentialClientApplicationBuilder ccaBuilder = ConfidentialClientApplicationBuilder
                .Create(s_clientIdForConfidentialApp)
                .WithAuthority(s_ccaAuthority)
                .WithCertificate(s_confidentialClientCertificate);

            IConfidentialClientApplication ccapp = ccaBuilder.Build();

            // Optionally set cache settings or other configurations if needed
            // cca.AppTokenCache.SetBeforeAccess((t) => { });

            return ccapp;
        }

        private static IConfidentialClientApplication CreateCcaForMtlsPop(string region)
        {
            ConfidentialClientApplicationBuilder ccaBuilder = ConfidentialClientApplicationBuilder
                .Create("163ffef9-a313-45b4-ab2f-c7e2f5e0e23e")
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion(region);

            ccaBuilder = ccaBuilder.WithCertificate(s_confidentialClientCertificate, true);

            //Add Experimental feature for MTLS PoP
            ccaBuilder = ccaBuilder.WithExperimentalFeatures();

            IConfidentialClientApplication ccapp = ccaBuilder.Build();

            // Optionally set cache settings or other configurations if needed
            // cca.AppTokenCache.SetBeforeAccess((t) => { });

            return ccapp;
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

        private static string GetPasswordFromConsole()
        {
            Console.Write("Password: ");
            string pwd = "";

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
                        pwd.Remove(pwd.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                {
                    pwd += i.KeyChar;
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
#pragma warning disable CS0618 // Type or member is obsolete
                        Task<AuthenticationResult> authenticationResultTask = Task.Run(() =>
                            AcquireTokenBuilder
                                .WithAuthority(s_ccaAuthority, true)
                                .ExecuteAsync());
#pragma warning restore CS0618 // Type or member is obsolete

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
