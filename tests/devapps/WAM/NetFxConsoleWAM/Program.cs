// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.Desktop;

namespace NetFxConsoleWAM
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
        private static readonly string s_authority = "https://login.microsoftonline.com/common/";

        static async Task Main(string[] args)
        {
            var pca = CreatePublicClientForRuntime();
            IntPtr hWnd = GetConsoleWindow();

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
            var acc = accounts.FirstOrDefault();
            AuthenticationResult result = null;

            try
            {
                result = await pca
                    .AcquireTokenSilent(s_scopes, acc)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    result = await pca
                        .AcquireTokenInteractive(s_scopes)
                        .WithParentActivityOrWindow(hWnd)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Console.WriteLine(result.AccessToken);
                }
                catch(MsalException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.Read();
        }

       private static IPublicClientApplication CreatePublicClientForRuntime()
        {
            var pca = PublicClientApplicationBuilder.Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
                .WithAuthority(s_authority)
                .WithBroker()
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))                
                .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                .Build();

            return pca;
        }
    }
}

