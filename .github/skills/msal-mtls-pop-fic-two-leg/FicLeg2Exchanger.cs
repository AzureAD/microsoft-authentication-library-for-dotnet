// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Production helper for Leg 2 assertion exchange in FIC two-leg flows.
    /// Uses assertion from Leg 1 with certificate binding to acquire tokens for target resources.
    /// </summary>
    /// <remarks>
    /// Based on test code: ClientCredentialsMtlsPopTests.Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync (Leg 2)
    /// </remarks>
    public class FicLeg2Exchanger
    {
        private readonly IConfidentialClientApplication _app;
        private readonly string _assertionJwt;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicLeg2Exchanger"/> class.
        /// </summary>
        /// <param name="clientId">Azure AD application (client) ID.</param>
        /// <param name="tenantId">Azure AD tenant ID or domain name.</param>
        /// <param name="assertionJwt">JWT token from Leg 1 to use as assertion.</param>
        /// <param name="bindingCertificate">X.509 certificate to bind the assertion (enables jwt-pop).</param>
        /// <param name="azureRegion">Optional Azure region for regional endpoints (e.g., "westus3").</param>
        public FicLeg2Exchanger(
            string clientId,
            string tenantId,
            string assertionJwt,
            X509Certificate2 bindingCertificate,
            string azureRegion = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (string.IsNullOrEmpty(assertionJwt))
                throw new ArgumentException("Assertion JWT cannot be null or empty.", nameof(assertionJwt));
            if (bindingCertificate == null)
                throw new ArgumentNullException(nameof(bindingCertificate));

            _assertionJwt = assertionJwt;

            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithExperimentalFeatures() // Required for WithClientAssertion
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    // Return assertion with certificate binding for jwt-pop
                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = assertionJwt,                       // Leg 1 token
                        TokenBindingCertificate = bindingCertificate    // Enables jwt-pop
                    });
                });

            if (!string.IsNullOrEmpty(azureRegion))
            {
                builder.WithAzureRegion(azureRegion);
            }

            _app = builder.Build();
        }

        /// <summary>
        /// Acquires a token for the specified scopes using the assertion from Leg 1.
        /// MSAL automatically sets client_assertion_type=jwt-pop when TokenBindingCertificate is provided.
        /// </summary>
        /// <param name="scopes">Target resource scopes (e.g., "https://vault.azure.net/.default").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="withPoP">If true, requests mTLS PoP token for Leg 2. Default is true (recommended).</param>
        /// <returns>An <see cref="AuthenticationResult"/> with TokenType="mtls_pop" (if withPoP=true).</returns>
        /// <exception cref="MsalServiceException">Thrown for Azure AD service errors (invalid_grant, unauthorized_client, etc.).</exception>
        /// <exception cref="MsalClientException">Thrown for client-side errors.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string[] scopes,
            CancellationToken cancellationToken = default,
            bool withPoP = true)
        {
            if (scopes == null || scopes.Length == 0)
                throw new ArgumentException("Scopes cannot be null or empty.", nameof(scopes));

            var request = _app.AcquireTokenForClient(scopes);

            if (withPoP)
            {
                request.WithMtlsProofOfPossession(); // Request mTLS PoP token
            }

            AuthenticationResult result = await request
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Validate PoP token type if requested
            if (withPoP && result.TokenType != "mtls_pop")
            {
                throw new InvalidOperationException(
                    $"Expected token type 'mtls_pop', but got '{result.TokenType}'. " +
                    "Ensure TokenBindingCertificate is set in ClientSignedAssertion.");
            }

            return result;
        }

        /// <summary>
        /// Clears the token cache for Leg 2.
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
