using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AdalCoreCLRTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AcquireTokenAsync().Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine(ae.InnerException.Message);
                Console.WriteLine(ae.InnerException.StackTrace);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static async Task AcquireTokenAsync()
        {
            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common", true);
            var certificate = GetCertificateByThumbprint("<CERT_THUMBPRINT>");
            var result = await context.AcquireTokenAsync("https://graph.windows.net", new ClientAssertionCertificate("<CLIENT_ID>", certificate));

            string token = result.AccessToken;
            Console.WriteLine(token + "\n");
        }

        private static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count > 0)
                {
                    return certs[0];
                }
                throw new Exception($"Cannot find certificate with thumbprint '{thumbprint}'");
            }
        }
    }
}