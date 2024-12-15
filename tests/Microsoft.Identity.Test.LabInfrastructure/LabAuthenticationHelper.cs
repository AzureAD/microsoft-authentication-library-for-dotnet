// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Unit;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    internal static class LabAuthenticationHelper
    {
        public static async Task<AccessToken> GetAccessTokenForLabAPIAsync()
        {
            string[] scopes = [LabApiConstants.LabScope];

            return await GetLabAccessTokenAsync(
                LabApiConstants.LabClientInstance + LabApiConstants.LabClientTenantId, 
                scopes).ConfigureAwait(false);
        }

        public static async Task<AccessToken> GetKeyVaultAccessToken()
        {
            var accessToken = await GetLabAccessTokenAsync(
              "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/",
              new[] { "https://vault.azure.net/.default" }).ConfigureAwait(false);

            return accessToken;
        }

        private static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes)
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert;

            cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(LabApiConstants.LabClientId)
                .WithAuthority(new Uri(authority), true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithCertificate(cert, true)
                .Build();

            authResult = await confidentialApp
                .AcquireTokenForClient(scopes)
                .WithSendX5C(true)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return new AccessToken(authResult.AccessToken, authResult.ExpiresOn);
        }
    }

}
