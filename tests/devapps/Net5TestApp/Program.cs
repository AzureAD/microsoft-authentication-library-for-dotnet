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
        private const string AuthorityV1 = "https://sammyciam.ciamextensibility.com/4710d5e4-43bb-4ff9-89af-30ed8fe31c6d/";
        private const string AuthorityV2 = "https://sammyciam.ciamextensibility.com/4710d5e4-43bb-4ff9-89af-30ed8fe31c6d/v2.0/";

        private const string AuthorityV1DC = "https://sammyciam.ciamextensibility.com/4710d5e4-43bb-4ff9-89af-30ed8fe31c6d?DC=ESTS-PUB-SCUS-LZ1-FD000-TEST1";
        private const string AuthorityV2DC = "https://sammyciam.ciamextensibility.com/4710d5e4-43bb-4ff9-89af-30ed8fe31c6d/v2.0?DC=ESTS-PUB-SCUS-LZ1-FD000-TEST1";

        private const string DC = "dc=ESTS-PUB-SCUS-LZ1-FD000-TEST1";
        private const string Scope = "api://2a247857-5770-477e-a9c0-a5f6bc8e66db/Scope2";

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
                //.WithAuthority(AuthorityV1)
                .WithExtraQueryParameters(DC)
                .WithGenericAuthority(AuthorityV2DC)
                //.WithInstanceDiscovery(false)                
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
