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

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabAuthenticationHelper
    {
        public const string LabAccessConfidentialClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        public const string LabScope = "https://request.msidlab.com/.default";
        public const string LabClientInstance = "https://login.microsoftonline.com/";
        public const string LabClientTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        public static async Task<AccessToken> GetAccessTokenForLabAPIAsync(string labAccessClientId)
        {
            string[] scopes = new string[] { LabScope };

            return await GetLabAccessTokenAsync(
                LabClientInstance + LabClientTenantId, 
                scopes,  
                labAccessClientId).ConfigureAwait(false);
        }

        public static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes)
        {
            return await GetLabAccessTokenAsync(
                authority,
                scopes,
                String.Empty).ConfigureAwait(false);
        }

        public static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes, string clientId)
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert;

            var clientIdForCertAuth = String.IsNullOrEmpty(clientId) ? LabAccessConfidentialClientId : clientId;

            cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(clientIdForCertAuth)
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

    public enum LabAccessAuthenticationType
    {
        ClientCertificate,
        ClientSecret,
        UserCredential
    }
}
