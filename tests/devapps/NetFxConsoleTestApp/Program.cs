//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace NetCoreTestApp
{
    public class Program
    {
        // TODO: replace with FOCI family members IDs
        private const string FAMILY_MEMBER_1 = ""; 
        private const string FAMILY_MEMBER_2 = ""; 

        private static readonly string[] s_scopes = new[] { "https://graph.microsoft.com/.default" };

        private static readonly string s_cacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin";

        // These 2 apps share the cache
        private static IPublicClientApplication s_pca1;
        private static IPublicClientApplication s_pca2;

        public static void Main(string[] args)
        {
            s_pca1 = PublicClientApplicationBuilder
                .Create(FAMILY_MEMBER_1)
                .WithLogging(Log, LogLevel.Verbose, true)
                .Build();

            s_pca2 = PublicClientApplicationBuilder
               .Create(FAMILY_MEMBER_2)
               .WithLogging(Log, LogLevel.Verbose, true)
               .Build();


            SetCacheSerializationToFile(s_pca1);

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

                await DisplayAccountsAsync(s_pca1).ConfigureAwait(false);

                // display menu
                Console.WriteLine(@"
                        1. Acquire Token App1
                        2. Acquire Token App2
                        3. Acquire Token Silent App1
                        4. Acquire Token Silent App2                        
                        5. Clear cache
                        0. Exit App
                    Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);

                Task<AuthenticationResult> authTask = null;

                try
                {
                    switch (selection)
                    {
                        case 1:
                            authTask = GetInteractiveAuthTaskAsync(s_pca1);
                            FetchTokenAsync(s_pca2, authTask).GetAwaiter().GetResult();
                            break;
                        case 2:
                            authTask = GetInteractiveAuthTaskAsync(s_pca2);
                            FetchTokenAsync(s_pca2, authTask).GetAwaiter().GetResult();
                            break;
                        case 3:
                            authTask = GetSilentAuthTaskAsync(s_pca1);
                            FetchTokenAsync(s_pca1, authTask).GetAwaiter().GetResult();
                            break;
                        case 4:
                            authTask = GetSilentAuthTaskAsync(s_pca2);
                            FetchTokenAsync(s_pca2, authTask).GetAwaiter().GetResult();
                            break;

                        case 5:
                            var accounts = await s_pca1.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts)
                            {
                                await s_pca1.RemoveAsync(acc).ConfigureAwait(false);
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

        private static Task<AuthenticationResult> GetInteractiveAuthTaskAsync(IPublicClientApplication pca)
        {
            return pca.AcquireTokenInteractive(s_scopes, null).ExecuteAsync();
        }


        private static Task<AuthenticationResult> GetSilentAuthTaskAsync(IPublicClientApplication pca)
        {
            var accounts = pca.GetAccountsAsync().GetAwaiter().GetResult();
            if (accounts.Count() > 1)
            {
                Log(LogLevel.Error, "Not expecting to handle multiple accounts", false);
                return null;
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
