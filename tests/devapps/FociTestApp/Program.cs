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

namespace FociTestApp
{
    public class Program
    {
        private const string FAMILY_MEMBER_1 = "7660e4d6-d3f3-4385-9851-bc9027ef4a03";
        private const string FAMILY_MEMBER_2 = "9668f2bd-6103-4292-9024-84fa2d1b6fb2";
        private const string NON_FAMILY_MEMBER = "0615b6ca-88d4-4884-8729-b178178f7c27";
        private static bool s_useIWA = false;

        private static readonly string[] s_scopes = new[] { "user.read" };

        private static readonly string s_cacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.json";

        // These 2 apps share the cache
        private static IPublicClientApplication s_pcaFam1;
        private static IPublicClientApplication s_pcaFam2;
        private static IPublicClientApplication s_pcaNonFam;

        public static void Main(string[] args)
        {
            s_pcaFam1 = PublicClientApplicationBuilder
                .Create(FAMILY_MEMBER_1)
                .WithRedirectUri("http://localhost")
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithLogging(Log, LogLevel.Verbose, true)
                .Build();

            s_pcaFam2 = PublicClientApplicationBuilder
               .Create(FAMILY_MEMBER_2)
               .WithRedirectUri("http://localhost")
               .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
               .WithLogging(Log, LogLevel.Verbose, true)
               .Build();

            s_pcaNonFam = PublicClientApplicationBuilder
             .Create(NON_FAMILY_MEMBER)
             .WithRedirectUri("http://localhost")
             .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
             .WithLogging(Log, LogLevel.Verbose, true)
             .Build();

            SetCacheSerializationToFile(s_pcaFam1);
            SetCacheSerializationToFile(s_pcaFam2);
            SetCacheSerializationToFile(s_pcaNonFam);

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

                await DisplayAccountsAsync(s_pcaFam1).ConfigureAwait(false);

                // display menu
                Console.WriteLine($@"
                        1. Acquire Token App1 (family member)
                        2. Acquire Token App2 (family member)
                        3. Acquire Token App3 (non-family member)
                        4. Acquire Token Silent App1 (family member)
                        5. Acquire Token Silent App2 (family member)
                        6. Acquire Token Silent App3 (non-family member)


                        7. Clear cache via App1
                        8. Clear cache via App2
                        t. Toggle IWA (currently {(s_useIWA ? "ON" : "OFF")})
                        0. Exit App
                    Enter your Selection: ");
                char.TryParse(Console.ReadLine(), out var selection);
                Task<AuthenticationResult> authTask;

                try
                {
                    switch (selection)
                    {
                        case '1':
                            authTask = StartAcquireTokenAsync(s_pcaFam1);
                            FetchTokenAsync(s_pcaNonFam, authTask).GetAwaiter().GetResult();
                            break;
                        case '2':
                            authTask = StartAcquireTokenAsync(s_pcaFam2);
                            FetchTokenAsync(s_pcaNonFam, authTask).GetAwaiter().GetResult();
                            break;
                        case '3':
                            authTask = StartAcquireTokenAsync(s_pcaNonFam);
                            FetchTokenAsync(s_pcaNonFam, authTask).GetAwaiter().GetResult();
                            break;
                        case '4':
                            authTask = StartSilentAuthAsync(s_pcaFam1);
                            FetchTokenAsync(s_pcaFam1, authTask).GetAwaiter().GetResult();
                            break;
                        case '5':
                            authTask = StartSilentAuthAsync(s_pcaFam2);
                            FetchTokenAsync(s_pcaNonFam, authTask).GetAwaiter().GetResult();
                            break;
                        case '6':
                            authTask = StartSilentAuthAsync(s_pcaNonFam);
                            FetchTokenAsync(s_pcaNonFam, authTask).GetAwaiter().GetResult();
                            break;

                        case '7':
                            var accounts1 = await s_pcaFam1.GetAccountsAsync().ConfigureAwait(false);
                            var accounts2 = await s_pcaFam2.GetAccountsAsync().ConfigureAwait(false);
                            var accounts3 = await s_pcaNonFam.GetAccountsAsync().ConfigureAwait(false);

                            foreach (var acc in accounts1)
                            {
                                await s_pcaFam1.RemoveAsync(acc).ConfigureAwait(false);
                            }

                            break;
                        case '8':
                            accounts1 = await s_pcaFam1.GetAccountsAsync().ConfigureAwait(false);
                            accounts2 = await s_pcaFam2.GetAccountsAsync().ConfigureAwait(false);
                            accounts3 = await s_pcaNonFam.GetAccountsAsync().ConfigureAwait(false);

                            foreach (var acc in accounts2)
                            {
                                await s_pcaFam2.RemoveAsync(acc).ConfigureAwait(false);
                            }

                            break;
                        case '0':
                            return;
                        case 't':
                            s_useIWA = true;
                            break;
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

        private static Task<AuthenticationResult> StartAcquireTokenAsync(IPublicClientApplication pca)
        {
            if (s_useIWA)
            {
                return pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).ExecuteAsync();
            }
            return pca.AcquireTokenInteractive(s_scopes).WithUseEmbeddedWebView(false).ExecuteAsync();
        }

        private static Task<AuthenticationResult> StartSilentAuthAsync(IPublicClientApplication pca)
        {
            // get all serialized accounts
            // get all RTs WHERE rt.client == app.client OR app is part of family or unknown
            // JOIN accounts and RTs ON homeAccountID

            // A -> interactive auth -> account, RT1
            // B -> GetAccounts -> NULL

            var accounts = pca.GetAccountsAsync().GetAwaiter().GetResult();
            if (accounts.Count() > 1)
            {
                Log(LogLevel.Error, "Not expecting to handle multiple accounts", false);
                return Task.FromResult<AuthenticationResult>(null);
            }

            return pca.AcquireTokenSilent(s_scopes, accounts.FirstOrDefault()).ExecuteAsync();
        }

        private static async Task FetchTokenAsync(IPublicClientApplication pca, Task<AuthenticationResult> authTask)
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
            await DisplayAccountsAsync(pca).ConfigureAwait(false);

            Console.ResetColor();
        }

        private static async Task DisplayAccountsAsync(IPublicClientApplication pca)
        {
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "For the public client, the tokenCache contains {0} token(s)", accounts.Count()));

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
