// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Net5TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var result = await TryAuthAsync().ConfigureAwait(false);
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Access Token = " + result?.AccessToken);
                Console.ResetColor();
            }
            catch (MsalException e)
            {
                Console.WriteLine("Error: ErrorCode=" + e.ErrorCode + "ErrorMessage=" + e.Message);
                Console.ResetColor();
            }

            Console.Read();
        }

        private static async Task<AuthenticationResult> TryAuthAsync()
        {
            var pca = PublicClientApplicationBuilder.
                Create("fe67c0cf-caae-49f0-9f75-e3f7e1e28724")
                .WithExperimentalFeatures(true)
                .WithExtraQueryParameters(DC)
                .WithRedirectUri("http://localhost")
                .Build();

            var result = await pca.AcquireTokenInteractive(new[] { Scope })
                .WithUseEmbeddedWebView(true)
                .WithPrompt(Prompt.Create)
                .ExecuteAsync()
                .ConfigureAwait(false);

            var account = (await pca.GetAccountsAsync().ConfigureAwait(false)).First();

            var result2 = await pca.AcquireTokenSilent(new[] { Scope }, account)
               .WithForceRefresh(true)
               .ExecuteAsync()
               .ConfigureAwait(false);


            return result;
        }

        
    }
}
