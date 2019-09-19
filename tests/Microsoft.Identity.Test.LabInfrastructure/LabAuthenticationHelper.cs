using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabAuthenticationHelper
    {
        private static InMemoryTokenCache s_staticCache = new InMemoryTokenCache();
        private const string LabAccessConfidentialClientId = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string LabAccessPublicClientId = "3c1e0e0d-b742-45ba-a35e-01c664e14b16";
        private const string LabAccessThumbPrint = "79FBCBEB5CD28994E50DAFF8035BACF764B14306";
        private const string DataFileName = "data.txt";
        private static LabAccessAuthenticationType s_defaultAuthType = LabAccessAuthenticationType.ClientCertificate;
        private static string s_secret;

        static LabAuthenticationHelper()
        {
            //The data.txt is a place holder for the keyvault secret. It will only be written to during build time when testing appcenter.
            //After the tests are finished in appcenter, the file will be deleted from the appcenter servers.
            //The file will then be deleted locally Via VSTS task.
            if (File.Exists(DataFileName))
            {
                var data = File.ReadAllText(DataFileName);

                if (!string.IsNullOrWhiteSpace(data))
                {
                    s_defaultAuthType = LabAccessAuthenticationType.ClientSecret;
                    s_secret = data;
                }
            }
        }

        public static async Task<string> GetAccessTokenForLabAPIAsync(string labAccessClientId, string labAccessSecret)
        {
            string[] scopes = new string[] { "https://request.msidlab.com/.default" };

            return await GetLabAccessTokenAsync(
                "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/", 
                scopes, 
                LabAccessAuthenticationType.ClientSecret, 
                labAccessClientId, 
                String.Empty, 
                labAccessSecret).ConfigureAwait(false);
        }

        public static async Task<string> GetLabAccessTokenAsync(string authority, string[] scopes)
        {
            return await GetLabAccessTokenAsync(
                authority,
                scopes,
                s_defaultAuthType,
                String.Empty,
                String.Empty,
                String.Empty).ConfigureAwait(false);
        }

        public static async Task<string> GetLabAccessTokenAsync(string authority, string[] scopes, LabAccessAuthenticationType authType, string clientId, string certThumbprint, string clientSecret)
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert;

            switch (authType)
            {
                case LabAccessAuthenticationType.ClientCertificate:
                    var clientIdForCertAuth = String.IsNullOrEmpty(clientId) ? LabAccessConfidentialClientId : clientId;
                    var certThumbprintForLab = String.IsNullOrEmpty(clientId) ? LabAccessThumbPrint : certThumbprint;

                    cert = CertificateHelper.FindCertificateByThumbprint(certThumbprintForLab);
                    if (cert == null)
                    {
                        throw new InvalidOperationException(
                            "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
                    }

                    confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientIdForCertAuth)
                        .WithAuthority(new Uri(authority), true)
                        .WithCertificate(cert)
                        .Build();

                    s_staticCache.Bind(confidentialApp.AppTokenCache);

                    authResult = await confidentialApp
                        .AcquireTokenForClient(scopes)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    break;
                case LabAccessAuthenticationType.ClientSecret:
                    var clientIdForSecretAuth = String.IsNullOrEmpty(clientId) ? LabAccessConfidentialClientId : clientId;
                    var clientSecretForLab = String.IsNullOrEmpty(clientId) ? s_secret : clientSecret;

                    confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientIdForSecretAuth)
                        .WithAuthority(new Uri(authority), true)
                        .WithClientSecret(clientSecretForLab)
                        .Build();
                    s_staticCache.Bind(confidentialApp.AppTokenCache);

                    authResult = await confidentialApp
                        .AcquireTokenForClient(scopes)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    break;
                case LabAccessAuthenticationType.UserCredential:
                    var clientIdForPublicClientAuth = String.IsNullOrEmpty(clientId) ? LabAccessPublicClientId : clientId;
                    var publicApp = PublicClientApplicationBuilder
                        .Create(clientIdForPublicClientAuth)
                        .WithAuthority(new Uri(authority), true)
                        .Build();
                    s_staticCache.Bind(publicApp.UserTokenCache);

                    authResult = await publicApp
                        .AcquireTokenByIntegratedWindowsAuth(scopes)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return authResult?.AccessToken;
        }
    }

    public enum LabAccessAuthenticationType
    {
        ClientCertificate,
        ClientSecret,
        UserCredential
    }
}
