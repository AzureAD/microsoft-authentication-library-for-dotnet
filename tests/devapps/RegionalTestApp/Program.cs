using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

/**
 * Note: Restart the application between the test cases to clear the statics.
 * 
 * Case 1: Regional with a valid user-specified region
 *  Look for:
 *      [Region discovery] Region found in environment variable: centralus.
 *      [Region discovery] Returning user provided region: westus.
 *      [HttpManager] Sending request. Method: POST. URI: https://westus.login.microsoft.com...
 *      Fetched access token from host westus.login.microsoft.com. Endpoint https://westus.login.microsoft.com...
 * 
 * Case 2: Regional auto-detect with environment variable
 *  Look for:
 *      [Region discovery] Region found in environment variable: centralus.
 *      [Region discovery] Auto-discovery already ran and found centralus
 *      [HttpManager] Sending request. Method: POST. URI: https://centralus.login.microsoft.com...
 *      Fetched access token from host centralus.login.microsoft.com. Endpoint https://centralus.login.microsoft.com...
 *      
 * Case 3: Regional auto-detect with IMDS call (without environment variable, test in Azure VM)
 *  On Azure VM:
 *      [Region discovery] Call to local IMDS succeeded. Region: {VM-region}.
 *      [HttpManager] Sending request. Method: POST. URI: https://{VM-region}.login.microsoft.com
 *      Fetched access token from host {VM-region}.login.microsoft.com. Endpoint https://{VM-region}.login.microsoft.com
 *  On non-Azure VM:
 *      [Region discovery] IMDS call failed...
 *      [HttpManager] Sending request. Method: POST. URI: https://login.microsoftonline.com
 *      Fetched access token from host login.microsoftonline.com. Endpoint https://login.microsoftonline.com...
 * 
 * Case 4: Acquire token with global, then regional 
 *  Look for:
 *      Global instance discovery
 *      Token #1 request to and retrieved from a global endpoint
 *      Region retrieved from the environment variable
 *      Regional instance discovery
 *      Token #2 request to and retrieved from a regional endpoint
 * 
 * Case 5: Acquire token with regional, then global 
 *  Look for:
 *      Region retrieved from the environment variable
 *      Regional instance discovery
 *      Token #1 request to and retrieved from a regional endpoint
 *      Global instance discovery
 *      Token #2 request to and retrieved from a global endpoint
 * 
 * Case 6: Acquire token twice in a row with regional (tests for double region appending regression)
 *  Look for:
 *      Region retrieved from the environment variable
 *      Regional instance discovery
 *      Token #1 request to and retrieved from a regional endpoint
 *      Token #2 request to and retrieved from a regional endpoint
 * 
 * Case 7: Acquire token twice in a row with global
 *  Look for:
 *      Global instance discovery
 *      Token #1 request to and retrieved from a global endpoint
 *      Token #2 request to and retrieved from a global endpoint
 *      
 * Case 8: Regional with an invalid user-specified region.
 *  Look for:
 *      Retrieved region from the environment variable
 *      Returns user-provided region
 *      Sends request to a user-provided region (gateway forwards to global internally)
 *      Token received from a user-provided region
 * 
 * General tips:
 * - Run this app from an Azure VM to test the IMDS call
 * - Make sure to reference the MSAL NuGet release package when testing
 * - Make sure verbose and PII logging is enabled
 * - Make sure the Lab certificate is installed
 *    Sample PowerShell command: Import-PfxCertificate -FilePath "C:\cert\cert.pfx" -CertStoreLocation Cert:\CurrentUser\My
 * - You can use Fiddler to verify the token endpoint
 **/
namespace TestApp
{
    public class Program
    {
        private const string s_environmentVarName = "REGION_NAME";
        private const string s_region = "centralus";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Naming", "UseAsyncSuffix:Use Async suffix", Justification = "Main method")]
        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine(@"
    1: Regional with a valid user-specified region
    2: Regional with environment variable
    3: Regional with IMDS call (without environment variable, test in Azure VM)
    4: Acquire token with global, then regional
    5: Acquire token with regional, then global
    6: Acquire token twice in a row with regional (tests for double region appending regression)
    7: Acquire token twice in a row with global
    8: Regional with an invalid user-specified region
    0. Exit app
    Enter your selection: ");
                int.TryParse(Console.ReadLine(), out var menuSelection);

                try
                {
                    X509Certificate2 certificate = ReadCertificate();

                    switch (menuSelection)
                    {
                        case 1: // Regional with a valid user-specified region
                            await AcquireTokenAsync(certificate, region: "westus").ConfigureAwait(false);
                            break;

                        case 2: // Regional auto-detect with environment variable
                            await AcquireTokenAsync(certificate, region: ConfidentialClientApplication.AttemptRegionDiscovery).ConfigureAwait(false);
                            break;

                        case 3: // Regional auto-detect with IMDS call (without environment variable, test in Azure VM)
                            await AcquireTokenAsync(certificate, region: ConfidentialClientApplication.AttemptRegionDiscovery, setEnvVariable: false).ConfigureAwait(false);
                            break;

                        case 4: // Acquire token with global, then regional
                            await AcquireTokenAsync(certificate, region: string.Empty).ConfigureAwait(false);
                            await AcquireTokenAsync(certificate, region: ConfidentialClientApplication.AttemptRegionDiscovery).ConfigureAwait(false);
                            break;

                        case 5: // Acquire token with regional, then global
                            await AcquireTokenAsync(certificate, region: ConfidentialClientApplication.AttemptRegionDiscovery).ConfigureAwait(false);
                            await AcquireTokenAsync(certificate, region: string.Empty).ConfigureAwait(false);
                            break;

                        case 6: // Acquire token twice in a row with regional (tests for double region appending regression)
                            await AcquireTokenAsync(certificate, region: ConfidentialClientApplication.AttemptRegionDiscovery).ConfigureAwait(false);
                            await AcquireTokenAsync(certificate, region: ConfidentialClientApplication.AttemptRegionDiscovery).ConfigureAwait(false);
                            break;

                        case 7: // Acquire token twice in a row with global
                            await AcquireTokenAsync(certificate, region: string.Empty).ConfigureAwait(false);
                            await AcquireTokenAsync(certificate, region: string.Empty).ConfigureAwait(false);
                            break;

                        case 8: // Regional with an invalid user-specified region
                            await AcquireTokenAsync(certificate, region: "invalidRegion").ConfigureAwait(false);
                            break;

                        case 0:
                            return;

                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, ex.Message, false);
                    Log(LogLevel.Error, ex.StackTrace, false);
                }

                Console.WriteLine("\n\nHit 'ENTER' to exit...");
                Console.ReadLine();
            }
        }

        private static async Task AcquireTokenAsync(X509Certificate2 certificate, string region, bool setEnvVariable = true)
        {
            string clientId = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";

            if (!string.IsNullOrEmpty(region) && setEnvVariable)
            {
                Environment.SetEnvironmentVariable(s_environmentVarName, s_region);
            }

            string[] scopes = new string[] { $"{clientId}/.default", };
            Dictionary<string, string> dict = new Dictionary<string, string>
            {
                ["allowestsrnonmsi"] = "true"
            };

            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
                .WithCertificate(certificate)
                .WithLogging(Log, LogLevel.Verbose, true);

            // Regional if region specified, global otherwise
            if (!string.IsNullOrEmpty(region))
            {
                builder.WithAzureRegion(region);
            }

            var cca = builder.Build();

            Console.WriteLine($"CCA created. Is regional:{region}, Set env var:{setEnvVariable}.");

            AuthenticationResult result = await cca.AcquireTokenForClient(scopes)
                .WithExtraQueryParameters(dict)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Console.WriteLine("Access token:" + result.AccessToken);

            if (!string.IsNullOrEmpty(region) && setEnvVariable)
            {
                Environment.SetEnvironmentVariable(s_environmentVarName, null);
            }
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

        private static X509Certificate2 ReadCertificate()
        {
            string thumbprint = "97D8C9DB3C84874D0363DCA540778461B2291780";

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
    }
}
