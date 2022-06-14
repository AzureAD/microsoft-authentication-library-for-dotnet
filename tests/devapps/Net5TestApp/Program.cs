using System;
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
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Error: ErrorCode=" + e.ErrorCode + "ErrorMessage=" + e.Message);
                Console.ResetColor();
            }

            Console.Read();
        }

        private static async Task<AuthenticationResult> TryAuthAsync()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var pca = PublicClientApplicationBuilder.Create("04b07795-8ddb-461a-bbee-02f9e1bf7b46")
                 .WithTenantId("72f988bf-86f1-41af-91ab-2d7cd011db47")
                 .WithDefaultRedirectUri()
                 .WithLogging(MyLoggingMethod, LogLevel.Info, true, false)
#pragma warning restore CS0618 // Type or member is obsolete
                 .Build();

            var result = await pca.AcquireTokenInteractive(new[] { "https://storage.azure.com/.default" })
                .WithUseEmbeddedWebView(true)
                .ExecuteAsync().ConfigureAwait(false);

            return result;
        }

        static void MyLoggingMethod(LogLevel level, string message, bool containsPii)
        {
            Console.WriteLine($"MSALTest {level} {containsPii} {message}");
        }
    }
}
