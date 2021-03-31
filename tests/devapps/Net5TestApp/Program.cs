using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Net5TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var result = TryAuthAsync().Result;
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
            var pca = PublicClientApplicationBuilder.Create("16dab2ba-145d-4b1b-8569-bf4b9aed4dc8")
                .WithLogging(MyLoggingMethod, LogLevel.Info,
                       enablePiiLogging: true,
                       enableDefaultPlatformLogging: true)
                .WithDefaultRedirectUri()
                .Build();

            return await pca.AcquireTokenInteractive(new[] { "user.read" })
               .WithUseEmbeddedWebView(true)
               .ExecuteAsync().ConfigureAwait(false);
        }

        static void MyLoggingMethod(LogLevel level, string message, bool containsPii)
        {
            Console.WriteLine($"MSALTest {level} {containsPii} {message}");
        }
    }
}
