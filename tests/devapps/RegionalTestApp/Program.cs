using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

/**
 * 1. Create an Azure VM.
 * 2. Clone the repo on the Azure VM or Create a new console app and copy the below code to the console app.
 * 3. Add the nuget package for MSAL to test.
 * 5. Install the cert from lab 
 * Sample powershell command: Import-PfxCertificate -FilePath "C:\cert\cert.pfx" -CertStoreLocation Cert:\CurrentUser\My
 * 4. Run the app.
 * 
 * Case 1: Verify WithAzureRegion(true)
 * 1. Verify the logs for "WithAzureRegion: True"
 * 2. Verify the region was discovered "[Region discovery] Region: eastus"
 * 3. Verify the token is obtained from the regional endpoint
 * 4. You can use fiddler to verify the token endpoint
 * 
 * Case 2: Verify WithAzureRegion(false)
 * 1. Verify the logs for "WithAzureRegion: False"
 * 2. Verify the token is obtained from the global endpoint.
 **/
namespace TestApp
{
    class Program
    {
        static string s_clientId = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
#pragma warning disable UseAsyncSuffix // Use Async suffix
        static async Task Main(string[] args)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            //Uncomment below line to test in dev env.
            //Environment.SetEnvironmentVariable("REGION_NAME", "centralus");
            X509Certificate2 certificate = ReadCertificate("97D8C9DB3C84874D0363DCA540778461B2291780");

            Console.WriteLine("=== Acquire token regional ===");
            await AcquireTokenAsync(certificate).ConfigureAwait(false);
            Console.WriteLine();

            Console.WriteLine("=== Acquire token global ===");
            await AcquireTokenAsync(certificate, false).ConfigureAwait(false);

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

        private static X509Certificate2 ReadCertificate(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            X509Certificate2 cert = null;

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = store.Certificates;

                // Find unexpired certificates.
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                // From the collection of unexpired certificates, find the ones with the correct name.
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindByThumbprint, thumbprint, false);

                // Return the first certificate in the collection, has the right name and is current.
                cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            }
            return cert;
        }

        private static async Task AcquireTokenAsync(X509Certificate2 certificate, bool withAzureRegion = true)
        {
            string[] scopes = new string[] { $"{s_clientId}/.default", };
            Dictionary<string, string> dict = new Dictionary<string, string>
            {
                ["allowestsrnonmsi"] = "true"
            };

            var cca = ConfidentialClientApplicationBuilder.Create(s_clientId)
                .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
                .WithCertificate(certificate)
                .WithExperimentalFeatures(true)
                .WithLogging(Log, LogLevel.Info, true)
                .Build();

            Console.WriteLine("CCA created");

            AuthenticationResult result = await cca.AcquireTokenForClient(scopes)
            .WithPreferredAzureRegion(withAzureRegion, regionUsedIfAutoDetectFails: "centralus")
            .WithExtraQueryParameters(dict)
            .ExecuteAsync()
            .ConfigureAwait(false);

            Console.WriteLine("Access token:" + result.AccessToken);
        }
    }
}
