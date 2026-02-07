// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Production helper for acquiring Leg 1 tokens in FIC two-leg flows.
    /// Acquires a token with scope "api://AzureADTokenExchange/.default" to use as assertion in Leg 2.
    /// </summary>
    /// <remarks>
    /// Based on test code: ClientCredentialsMtlsPopTests.Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync (Leg 1)
    /// </remarks>
    public class FicLeg1Acquirer
    {
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";
        private readonly IConfidentialClientApplication _app;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicLeg1Acquirer"/> class.
        /// </summary>
        /// <param name="clientId">Azure AD application (client) ID.</param>
        /// <param name="tenantId">Azure AD tenant ID or domain name.</param>
        /// <param name="certificate">X.509 certificate with private key for client authentication.</param>
        /// <param name="azureRegion">Optional Azure region for regional endpoints (e.g., "westus3").</param>
        public FicLeg1Acquirer(
            string clientId,
            string tenantId,
            X509Certificate2 certificate,
            string azureRegion = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithCertificate(certificate, sendX5C: true); // sendX5C=true for PoP support

            if (!string.IsNullOrEmpty(azureRegion))
            {
                builder.WithAzureRegion(azureRegion);
            }

            _app = builder.Build();
        }

        /// <summary>
        /// Acquires a token exchange token (Leg 1) with scope "api://AzureADTokenExchange/.default".
        /// This token will be used as an assertion in Leg 2.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="withPoP">If true, requests mTLS PoP token for Leg 1. Default is true (recommended).</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the assertion JWT in AccessToken property.</returns>
        /// <exception cref="MsalServiceException">Thrown for Azure AD service errors.</exception>
        /// <exception cref="MsalClientException">Thrown for client-side errors.</exception>
        public async Task<AuthenticationResult> AcquireTokenExchangeTokenAsync(
            CancellationToken cancellationToken = default,
            bool withPoP = true)
        {
            var request = _app.AcquireTokenForClient(new[] { TokenExchangeScope });

            if (withPoP)
            {
                request.WithMtlsProofOfPossession(); // Enable PoP for Leg 1
            }

            AuthenticationResult result = await request
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(result.AccessToken))
            {
                throw new InvalidOperationException("Leg 1 did not return an access token.");
            }

            return result;
        }

        /// <summary>
        /// Clears the token cache for Leg 1.
        /// </summary>
        public async Task ClearCacheAsync()
        {
            var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                await _app.RemoveAsync(account).ConfigureAwait(false);
            }
        }
    }
}
