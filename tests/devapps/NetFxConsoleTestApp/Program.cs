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
//using NetStandard;

namespace NetFx
{
    public class Program
    {
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private static readonly string s_username = ""; // used for WIA and U/P, cannot be empty on .net core
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" }; // used for WIA and U/P, can be empty

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        private static IPublicClientApplication s_pca = null;
        public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.json";


        private static readonly string[] s_tids = new[]  {
            "common",
            "49f548d0-12b7-4169-a390-bb5304d24462",
            "72f988bf-86f1-41af-91ab-2d7cd011db47" };

        private static int s_currentTid = 0;



        public static void Main(string[] args)
        {
            s_pca = CreatePca();
            RunConsoleAppLogicAsync().Wait();
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

        private static async Task RunConsoleAppLogicAsync()
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("Authority: " + GetAuthority());
                await DisplayAccountsAsync(s_pca).ConfigureAwait(false);

                // display menu
                Console.WriteLine(@"
                        1. IWA
                        2. Acquire Token with Username and Password
                        3. Acquire Token with Device Code
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
                        authTask = s_pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).WithUsername(s_username).ExecuteAsync(CancellationToken.None);
                        await FetchTokenAndCallGraphAsync(s_pca, authTask).ConfigureAwait(false);

                        break;
                    case 2: // acquire token u/p
                        SecureString password = GetPasswordFromConsole();
                        authTask = s_pca.AcquireTokenByUsernamePassword(s_scopes, s_username, password).ExecuteAsync(CancellationToken.None);
                        await FetchTokenAndCallGraphAsync(s_pca, authTask).ConfigureAwait(false);

                        break;
                    case 3:
                        authTask = s_pca.AcquireTokenWithDeviceCode(
                            s_scopes,
                            deviceCodeResult =>
                            {
                                Console.WriteLine(deviceCodeResult.Message);
                                return Task.FromResult(0);
                            }).ExecuteAsync(CancellationToken.None);
                        await FetchTokenAndCallGraphAsync(s_pca, authTask).ConfigureAwait(false);

                        break;
                 
                    case 5: // acquire token interactive

                        CancellationTokenSource cts = new CancellationTokenSource();
                        authTask = s_pca.AcquireTokenInteractive(s_scopes)
                            //.WithUseEmbeddedWebView(false)
                            .ExecuteAsync(cts.Token);

                        await FetchTokenAndCallGraphAsync(s_pca, authTask).ConfigureAwait(false);

                        break;
                    case 6: // acquire token silent
                        IAccount account = s_pca.GetAccountsAsync().Result.FirstOrDefault();
                        if (account == null)
                        {
                            Log(LogLevel.Error, "Test App Message - no accounts found, AcquireTokenSilentAsync will fail... ", false);
                        }

                        authTask = s_pca.AcquireTokenSilent(s_scopes, account).ExecuteAsync(CancellationToken.None);
                        await FetchTokenAndCallGraphAsync(s_pca, authTask).ConfigureAwait(false);

                        break;
                    case 7:
                        //CancellationTokenSource cts2 = new CancellationTokenSource();
                        //var authenticator = new NetStandardAuthenticator(Log, CacheFilePath);
                        //await FetchTokenAndCallGraphAsync(s_pca, authenticator.GetTokenInteractiveAsync(cts2.Token)).ConfigureAwait(false);
                        break;
                    case 8:
                        var accounts = await s_pca.GetAccountsAsync().ConfigureAwait(false);
                        foreach (var acc in accounts)
                        {
                            await s_pca.RemoveAsync(acc).ConfigureAwait(false);
                        }

                        break;
                    case 9:

                        s_currentTid = (s_currentTid + 1) % s_tids.Length;
                        s_pca = CreatePca();

                        RunConsoleAppLogicAsync().Wait();
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

        private static async Task FetchTokenAndCallGraphAsync(IPublicClientApplication _pca, Task<AuthenticationResult> authTask)
        {
            await authTask.ConfigureAwait(false);

            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Token is {0}", authTask.Result.AccessToken);
            Console.ResetColor();


            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            await DisplayAccountsAsync(_pca).ConfigureAwait(false);
            Console.ResetColor();
        }



        private static async Task DisplayAccountsAsync(IPublicClientApplication _pca)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);

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

        //private static async Task<string> CallGraphAsync(string token)
        //{
        //    var httpClient = new HttpClient();
        //    System.Net.Http.HttpResponseMessage response;
        //    try
        //    {
        //        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, GraphAPIEndpoint);
        //        //Add the token in Authorization header
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //        response = await httpClient.SendAsync(request).ConfigureAwait(false);
        //        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        //        return content;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.ToString();
        //    }
        //}


    }
}
