// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// ServicePrincipalTokenProvider fetches an AAD token provided Service Principal credentials.
    /// </summary>
    internal class InternalServicePrincipalTokenProvider
    {
        private readonly IConfidentialClientApplication _client;

        internal InternalServicePrincipalTokenProvider(string authority, string tenantId, string clientId, string secret, IMsalHttpClientFactory clientFactory = null)
        {
            _client = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithTenantId(tenantId)
                .WithAuthority(new Uri(authority))
                .WithClientSecret(secret)
                .WithHttpClientFactory(clientFactory)
                .Build();
        }

        private InternalServicePrincipalTokenProvider(string authority, string tenantId, string clientId, X509Certificate2 cert, IMsalHttpClientFactory clientFactory)
        {
            _client = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithTenantId(tenantId)
                .WithAuthority(new Uri(authority))
                .WithCertificate(cert)
                .WithHttpClientFactory(clientFactory)
                .Build();
        }

        /// <summary>
        ///     ServicePrincipalCredentialProvider constructor to build the provider with a certificate
        /// </summary>
        /// <param name="authority">Hostname of the security token service (STS) from which MSAL.NET will acquire the tokens. Ex: login.microsoftonline.com
        /// </param>
        /// <param name="tenantId">A string representation for a GUID, which is the ID of the tenant where the account resides</param>
        /// <param name="clientId">A string representation for a GUID ClientId (application ID) of the application</param>
        /// <param name="cert">A ClientAssertionCertificate which is the certificate secret for the application</param>
        public InternalServicePrincipalTokenProvider(string authority, string tenantId, string clientId, X509Certificate2 cert)
            : this(authority, tenantId, clientId, cert, null)
        { }

        /// <summary>
        ///     GetTokenAsync returns a token for a given set of scopes
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="cancel">Cancellation token to cancel the HTTP token request</param>
        /// <returns>A token with expiration</returns>
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken cancel)
        {
            var res = await _client.AcquireTokenForClient(scopes)
                .ExecuteAsync(cancel)
                .ConfigureAwait(false);
            return new AccessTokenWithExpiration { ExpiresOn = res.ExpiresOn, AccessToken = res.AccessToken };
        }
    }
}
