using System;
using Microsoft.Identity.Client;

namespace CrossPlatformConsoleApp
{
    class Program
    {
        public static string ClientID = "4a1aa1d5-c567-49d0-ad0b-cd957a47f842"; //msidentity-samples-testing tenant

        public static string[] Scopes = { "User.Read" };

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var pcaBuilder = PublicClientApplicationBuilder.Create(ClientID)
                .WithRedirectUri("http://localhost")
                .Build();

            AcquireTokenInteractiveParameterBuilder atparamBuilder = pcaBuilder.AcquireTokenInteractive(Scopes);
            AuthenticationResult authenticationResult = atparamBuilder.ExecuteAsync().GetAwaiter().GetResult();
            
            System.Console.WriteLine(authenticationResult.AccessToken);
        }
    }
}
