// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;

namespace Net6WinConsoleWAM
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
        const string Authority = "https://login.microsoftonline.com/common/";

        //[Obsolete]
        public static async Task Main()
        {
            var pca = PublicClientApplicationBuilder.Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
                    .WithAuthority(Authority)
                    //.WithBroker(true)
                    .WithBroker()
                    //.WithWindowsBrokerOptions(new WindowsBrokerOptions() { HeaderText = "old" }) //This API will give warning. Replace as below.
                    .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows) {  Title = "new"})
                    .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                    .WithRedirectUri("http://localhost")
                    .WithParentActivityOrWindow(() => GetConsoleWindow())
                    .Build();

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
                        //.WithParentActivityOrWindow(hWnd)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Console.WriteLine(result.AccessToken);
                }
                catch (MsalException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.Read();
        }
    }
}
