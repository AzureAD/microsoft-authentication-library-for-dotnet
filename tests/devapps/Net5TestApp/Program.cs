using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Net5TestApp
{
  
    class Program
    {
#pragma warning disable UseAsyncSuffix // Use Async suffix
        static async Task Main(string[] args)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {            
            string clientId = Environment.GetEnvironmentVariable("LAB_APP_CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("LAB_APP_CLIENT_SECRET");
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            // the tenant id is important, especially for multi-tenant apps
            // single tenant apps could get away with setting "common", but this is strongly discouraged
            // because "common" + multi-tenant S2S app = non defined behavior
            string authority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47"; 

            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                   .Create(clientId)
                   .WithAuthority(authority)
                   .WithClientSecret(clientSecret) // today, a secret or a cert MUST be configured. Maybe we should relax this if MSI is to be used?
                   .Build();
            
            await (cca as ConfidentialClientApplication).InjectAppTokenAsync(scopes, 3600, "secret_at")
                .ConfigureAwait(false);

            AuthenticationResult result_msi = await cca.AcquireTokenForClient(scopes)                                    
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);

            Console.WriteLine($"Token {result_msi.AccessToken} from {result_msi.AuthenticationResultMetadata.TokenSource}");

            AuthenticationResult result_ests = await cca.AcquireTokenForClient(scopes)
                                    .WithForceRefresh(true)
                                   .ExecuteAsync()
                                   .ConfigureAwait(false);
           
            Console.WriteLine($"Token {result_ests.AccessToken} from {result_ests.AuthenticationResultMetadata.TokenSource}");


        }



    }
}
