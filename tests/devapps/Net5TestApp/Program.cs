// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using static System.Windows.Forms.Design.AxImporter;

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
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Error: ErrorCode=" + e.ErrorCode + "ErrorMessage=" + e.Message);
                Console.ResetColor();
            }

            Console.Read();
        }

        private static async Task<AuthenticationResult> TryAuthAsync()
        {
            var publicClientBuilder = PublicClientApplicationBuilder.Create("e3b9ad76-9763-4827-b088-80c7a7888f79");
            publicClientBuilder.WithB2CAuthority("https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_SISOPolicy/");
            var publicClient = publicClientBuilder.Build();

            // Gives exception only after upgrade to 4.48+
            var result = await publicClient.AcquireTokenByUsernamePassword(
                    new string[] { "user.read" },
                    "b2clocal@msidlabb2c.onmicrosoft.com",
                    "Tree1@342")
                .ExecuteAsync().ConfigureAwait(false);

            return result;

            //var pca = PublicClientApplicationBuilder.Create("04b07795-8ddb-461a-bbee-02f9e1bf7b46")
            //     .WithTenantId("72f988bf-86f1-41af-91ab-2d7cd011db47")
            //     .WithDefaultRedirectUri()
            //     .WithLogging(MyLoggingMethod, LogLevel.Info, true, false)
            //     .Build();

            //var result = await pca.AcquireTokenInteractive(new[] { "https://storage.azure.com/.default" })
            //    .WithUseEmbeddedWebView(true)
            //    .ExecuteAsync().ConfigureAwait(false);

            //return result;
        }

        static void MyLoggingMethod(LogLevel level, string message, bool containsPii)
        {
            Console.WriteLine($"MSALTest {level} {containsPii} {message}");
        }
    }
}
