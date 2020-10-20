using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

/**
 * 1. On an Azure VM, copy the code in a console app
 * 2. Run the app and verify the token is obtained.
 * 3. From the logs verify the token endpoint should contain the region. eg. centralus or eastus
 **/
namespace TestApp
{
    class Program
    {
        static string clientId = "<Lab_PublicCloudConfidentialClientID>";
#pragma warning disable UseAsyncSuffix // Use Async suffix
        static async Task Main(string[] args)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            var dict = new Dictionary<string, string>
            {
                ["allowestsrnonmsi"] = "true"
            };

            string secret = "Lab_Secret"; //Lab secret from TestConstants.MsalCCAKeyVaultUri

            //Uncomment below line to test in dev env.
            //Environment.SetEnvironmentVariable("REGION_NAME", "centralus");
            string[] scopes = new string[] { $"{clientId}/.default", };
            var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority("https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47")
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .WithLogging(Log, LogLevel.Info, true)
                .Build();

            Console.WriteLine("CCA created");
            
            var result = await cca.AcquireTokenForClient(scopes)
            .WithExtraQueryParameters(dict)
            .WithAzureRegion(true)
            .ExecuteAsync()
            .ConfigureAwait(false);

            Console.WriteLine("Token obtained");
            Console.ReadLine();
        }

        private static void Log(LogLevel level, string message, bool containsPii)
        {
            if (containsPii)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine($"{level} {message}");
            Console.ResetColor();
        }
    }
}
