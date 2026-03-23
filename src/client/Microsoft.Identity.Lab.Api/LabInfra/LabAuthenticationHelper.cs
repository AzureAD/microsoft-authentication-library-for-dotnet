// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// LabAuthenticationHelper provides utility methods for acquiring access tokens for Microsoft Identity lab authentication scenarios.
    /// </summary>
    public static class LabAuthenticationHelper
    {
        /// <summary>
        /// represents the client ID of the confidential client application used for Microsoft Identity lab authentication.
        /// </summary>
        public const string LabAccessConfidentialClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        /// <summary>
        /// represents the scope for Microsoft Identity lab API access. This scope is used when acquiring access tokens for authentication against Microsoft Identity lab services.
        /// </summary>
        public const string LabScope = "https://request.msidlab.com/.default";
        /// <summary>
        /// Represents the base URL for the Microsoft Online authentication endpoint used by lab client instances.
        /// </summary>
        public const string LabClientInstance = "https://login.microsoftonline.com/";
        /// <summary>
        /// represents the tenant ID for Microsoft Identity lab authentication. This tenant ID is used when constructing authority URLs for acquiring access tokens to authenticate against Microsoft Identity lab services.
        /// </summary>
        public const string LabClientTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        
        /// <summary>
        /// Certificate name for Microsoft Identity lab authentication. 
        /// This certificate is used for automated testing and is available only to Microsoft employees.
        /// </summary>
        private const string AutomationTestCertName = "LabAuth.MSIDLab.com";

        /// <summary>
        /// Gets an access token for the Microsoft Identity lab API using client credentials flow with certificate authentication.
        /// </summary>
        /// <param name="labAccessClientId"></param>
        /// <returns></returns>
        public static async Task<AccessToken> GetAccessTokenForLabAPIAsync(string labAccessClientId)
        {
            string[] scopes = new string[] { LabScope };

            return await GetLabAccessTokenAsync(
                LabClientInstance + LabClientTenantId, 
                scopes,  
                labAccessClientId).ConfigureAwait(false);
        }

        /// <summary>
        /// gets an access token for the Microsoft Identity lab API using client credentials flow with certificate authentication, using the default confidential client ID.
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes)
        {
            return await GetLabAccessTokenAsync(
                authority,
                scopes,
                String.Empty).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an access token for the Microsoft Identity lab API using client credentials flow with certificate authentication, allowing specification of authority, scopes, and client ID.
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="scopes"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes, string clientId)
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert;

            var clientIdForCertAuth = string.IsNullOrEmpty(clientId) ? LabAccessConfidentialClientId : clientId;

            cert = CertificateHelper.FindCertificateByName(AutomationTestCertName);
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

}
