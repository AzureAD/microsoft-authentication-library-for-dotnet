// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MultiCloudTestApp
{
    /// <summary>
    /// This test app simulates the VS user scenario which supports
    /// logging in from multiple accounts across multiple clouds.
    /// VS is registered with the same app id in each cloud.
    /// </summary>
    public class Program
    {
        // TODO: replace with an app that lives in multiple clouds, e.g. VS
        private const string APP_LIVING_IN_MULTIPLE_CLOUDS = "";

        private static readonly string[] s_scopesPublicCloud = new[] { "https://graph.microsoft.com/.default" };
        private static readonly string[] s_scopesDeCloud = new[] { "https://graph.cloudapi.de/.default" };
        private static readonly string[] s_scopesCnCloud = new[] { "https://microsoftgraph.chinacloudapi.cn/.default" };

        private static readonly string s_cacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.json";

        // These 2 apps share the cache
        private static IPublicClientApplication s_publicCloudApp;
        private static IPublicClientApplication s_deCloudApp;
        private static IPublicClientApplication s_cnCloudApp;

        public static void Main(string[] args)
        {
            s_publicCloudApp = PublicClientApplicationBuilder
                .Create(APP_LIVING_IN_MULTIPLE_CLOUDS)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                .WithLogging(Log, LogLevel.Verbose, true)
                .Build();

            s_deCloudApp = PublicClientApplicationBuilder
               .Create(APP_LIVING_IN_MULTIPLE_CLOUDS)
               .WithAuthority(AzureCloudInstance.AzureGermany, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
               .WithLogging(Log, LogLevel.Verbose, true)
               .Build();

            s_cnCloudApp = PublicClientApplicationBuilder
             .Create(APP_LIVING_IN_MULTIPLE_CLOUDS)
             .WithAuthority(AzureCloudInstance.AzureChina, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
             .WithLogging(Log, LogLevel.Verbose, true)
             .Build();

            SetCacheSerializationToFile(s_publicCloudApp);
            SetCacheSerializationToFile(s_deCloudApp);
            SetCacheSerializationToFile(s_cnCloudApp);

            RunConsoleAppLogicAsync().Wait();
        }

        private static void SetCacheSerializationToFile(IPublicClientApplication pca1)
        {
            pca1.UserTokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(s_cacheFilePath)
                    ? File.ReadAllBytes(s_cacheFilePath)
                    : null);
            });
            pca1.UserTokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(s_cacheFilePath, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
        }

        private static async Task RunConsoleAppLogicAsync()
        {
            while (true)
            {
                Console.Clear();

                await DisplayAllAccountsAsync().ConfigureAwait(false);

                // display menu
                Console.WriteLine(@"
                        1. Acquire Token App1 (public cloud)
                        2. Acquire Token App2 (Germany cloud)
                        3. Acquire Token App3 (China cloud)
                        4. Acquire Token Silent App1 (public cloud)
                        5. Acquire Token Silent App2 (Germany cloud)
                        6. Acquire Token Silent App3 (China cloud)


                        7. Clear cache
                        0. Exit App
                    Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);

                Task<AuthenticationResult> authTask = null;

                try
                {
                    switch (selection)
                    {
                    case 1:
                        FetchTokenAsync(
                            s_publicCloudApp,
                            s_publicCloudApp.AcquireTokenInteractive(s_scopesPublicCloud).ExecuteAsync())
                            .GetAwaiter().GetResult();
                        break;
                    case 2:
                        FetchTokenAsync(
                            s_deCloudApp,
                            s_deCloudApp.AcquireTokenInteractive(s_scopesDeCloud).ExecuteAsync())
                            .GetAwaiter().GetResult();
                        break;
                    case 3:
                        FetchTokenAsync(
                           s_cnCloudApp,
                            s_cnCloudApp.AcquireTokenInteractive(s_scopesCnCloud).ExecuteAsync())
                           .GetAwaiter().GetResult();
                        break;
                    case 4:
                        authTask = GetSilentAuthTaskAsync(s_publicCloudApp, s_scopesPublicCloud);
                        FetchTokenAsync(s_publicCloudApp, authTask).GetAwaiter().GetResult();
                        break;
                    case 5:
                        authTask = GetSilentAuthTaskAsync(s_deCloudApp, s_scopesDeCloud);
                        FetchTokenAsync(s_cnCloudApp, authTask).GetAwaiter().GetResult();
                        break;
                    case 6:
                        authTask = GetSilentAuthTaskAsync(s_cnCloudApp, s_scopesCnCloud);
                        FetchTokenAsync(s_cnCloudApp, authTask).GetAwaiter().GetResult();
                        break;

                    case 7:
                        var accountsPublic = await s_publicCloudApp.GetAccountsAsync().ConfigureAwait(false);
                        var accountsDe = await s_deCloudApp.GetAccountsAsync().ConfigureAwait(false);
                        var accountsCn = await s_cnCloudApp.GetAccountsAsync().ConfigureAwait(false);

                        foreach (var acc in accountsPublic)
                        {
                            await s_publicCloudApp.RemoveAsync(acc).ConfigureAwait(false);
                        }
                        foreach (var acc in accountsDe)
                        {
                            await s_deCloudApp.RemoveAsync(acc).ConfigureAwait(false);
                        }
                        foreach (var acc in accountsCn)
                        {
                            await s_cnCloudApp.RemoveAsync(acc).ConfigureAwait(false);
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

        private static async Task DisplayAllAccountsAsync()
        {
            Console.WriteLine("=== Public Cloud ====");
            await DisplayAccountsAsync(s_publicCloudApp).ConfigureAwait(false);

            Console.WriteLine("=== DE Cloud ====");
            await DisplayAccountsAsync(s_deCloudApp).ConfigureAwait(false);

            Console.WriteLine("=== CN Cloud ====");
            await DisplayAccountsAsync(s_cnCloudApp).ConfigureAwait(false);
        }

        private static Task<AuthenticationResult> GetSilentAuthTaskAsync(IPublicClientApplication pca, string[] scopes)
        {
            var accounts = pca.GetAccountsAsync().GetAwaiter().GetResult();
            if (accounts.Count() > 1)
            {
                Log(LogLevel.Error, "Not expecting to handle multiple accounts", false);
                return Task.FromResult<AuthenticationResult>(null);
            }

            return pca.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
        }

        private static async Task FetchTokenAsync(
            IPublicClientApplication pca,
            Task<AuthenticationResult> authTask)
        {
            if (authTask == null)
            {
                return;
            }

            await authTask.ConfigureAwait(false);

            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Token is {0}", authTask.Result.AccessToken);
            Console.ResetColor();

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            await DisplayAllAccountsAsync().ConfigureAwait(false);

            Console.ResetColor();
        }

        private static async Task DisplayAccountsAsync(IPublicClientApplication pca)
        {
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "For this cloud, the tokenCache contains {0} token(s)", accounts.Count()));

            foreach (var account in accounts)
            {
                Console.WriteLine("PCA account for: " + account.Username + "\n");
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

    }
}
