// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Handles Leg 2 of the FIC two-leg token exchange: exchanging Leg 1's token for a final
    /// resource token using Confidential Client with WithClientAssertion.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Leg 2 ALWAYS requires Confidential Client. Managed Identity does NOT have
    /// the WithClientAssertion() API and cannot perform Leg 2.
    /// 
    /// Supports two output token types:
    /// - Bearer: Omit TokenBindingCertificate in assertion
    /// - mTLS PoP: Include Leg 1's BindingCertificate in assertion + call WithMtlsProofOfPossession()
    /// </remarks>
    public sealed class FicLeg2Exchanger : IDisposable
    {
        private readonly IConfidentialClientApplication _app;
        private readonly FicAssertionProvider _assertionProvider;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicLeg2Exchanger"/> class.
        /// </summary>
        /// <param name="clientId">The application (client) ID for Leg 2.</param>
        /// <param name="authority">The authority URL (e.g., https://login.microsoftonline.com/tenant-id).</param>
        /// <param name="region">The Azure region for regional endpoints (e.g., "westus3").</param>
        /// <param name="leg1Result">The authentication result from Leg 1.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public FicLeg2Exchanger(
            string clientId,
            string authority,
            string region,
            AuthenticationResult leg1Result)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(authority);
            ArgumentNullException.ThrowIfNull(region);
            ArgumentNullException.ThrowIfNull(leg1Result);

            _assertionProvider = new FicAssertionProvider(leg1Result);

            _app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(authority)
                .WithAzureRegion(region)
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    // Default to Bearer assertion; specific methods override via _currentAssertionMode
                    return Task.FromResult(_assertionProvider.CreateBearerAssertion());
                })
                .Build();
        }

        /// <summary>
        /// Exchanges Leg 1 token for a Bearer token for the specified scopes.
        /// </summary>
        /// <param name="scopes">The target resource scopes (e.g., ["https://vault.azure.net/.default"]).</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The authentication result with a Bearer token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scopes is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public async Task<AuthenticationResult> ExchangeForBearerAsync(
            IEnumerable<string> scopes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scopes);
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Build a new app instance with Bearer assertion
            var bearerApp = ConfidentialClientApplicationBuilder
                .Create(_app.AppConfig.ClientId)
                .WithAuthority(_app.AppConfig.Authority)
                .WithAzureRegion(_app.AppConfig.AzureRegion)
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    return Task.FromResult(_assertionProvider.CreateBearerAssertion());
                })
                .Build();

            AuthenticationResult result = await bearerApp
                .AcquireTokenForClient(scopes)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Exchanges Leg 1 token for an mTLS PoP token for the specified scopes.
        /// Uses Leg 1's BindingCertificate for token binding.
        /// </summary>
        /// <param name="scopes">The target resource scopes (e.g., ["https://vault.azure.net/.default"]).</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The authentication result with an mTLS PoP token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scopes is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when Leg 1 result has no BindingCertificate.</exception>
        public async Task<AuthenticationResult> ExchangeForMtlsPopAsync(
            IEnumerable<string> scopes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scopes);
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Build a new app instance with mTLS PoP assertion
            var popApp = ConfidentialClientApplicationBuilder
                .Create(_app.AppConfig.ClientId)
                .WithAuthority(_app.AppConfig.Authority)
                .WithAzureRegion(_app.AppConfig.AzureRegion)
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    return Task.FromResult(_assertionProvider.CreateMtlsPopAssertion());
                })
                .Build();

            AuthenticationResult result = await popApp
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Gets the Leg 1 authentication result used for token exchange.
        /// </summary>
        public AuthenticationResult Leg1Result => _assertionProvider.Leg1Result;

        /// <summary>
        /// Disposes the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // MSAL application instances don't require explicit disposal
                _disposed = true;
            }
        }
    }
}
