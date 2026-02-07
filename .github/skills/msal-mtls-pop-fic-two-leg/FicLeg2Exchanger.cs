// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Client.Helpers
{
    /// <summary>
    /// Helper class for Leg 2 token exchange in FIC two-leg flows.
    /// Supports both Managed Identity and Confidential Client authentication.
    /// Can acquire Bearer or mTLS PoP tokens.
    /// </summary>
    /// <remarks>
    /// The application instance must be configured with:
    /// - .WithExperimentalFeatures()
    /// - .WithClientAssertion(...) providing ClientSignedAssertion from Leg 1
    /// </remarks>
    public class FicLeg2Exchanger : IDisposable
    {
        private readonly object _app;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with a Managed Identity application.
        /// </summary>
        /// <param name="app">
        /// The managed identity application, configured with .WithExperimentalFeatures() 
        /// and .WithClientAssertion(...).
        /// </param>
        public FicLeg2Exchanger(IManagedIdentityApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            _app = app;
            _isManagedIdentity = true;
        }

        /// <summary>
        /// Initializes a new instance with a Confidential Client application.
        /// </summary>
        /// <param name="app">
        /// The confidential client application, configured with .WithExperimentalFeatures() 
        /// and .WithClientAssertion(...).
        /// </param>
        public FicLeg2Exchanger(IConfidentialClientApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            _app = app;
            _isManagedIdentity = false;
        }

        /// <summary>
        /// Exchanges Leg 1 assertion for final token (Bearer or mTLS PoP).
        /// </summary>
        /// <param name="scopes">
        /// For MSI: Single scope string (e.g., "https://graph.microsoft.com/.default")
        /// For Confidential Client: Array of scopes (e.g., new[] { "https://vault.azure.net/.default" })
        /// </param>
        /// <param name="useMtlsPop">
        /// If true, acquires mTLS PoP token using .WithMtlsProofOfPossession().
        /// If false, acquires standard Bearer token.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication result with the final token.</returns>
        /// <exception cref="ArgumentException">If scopes format is invalid for the application type.</exception>
        public async Task<AuthenticationResult> ExchangeTokenAsync(
            object scopes,
            bool useMtlsPop,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(scopes);

            AuthenticationResult result;

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_app;
                var scope = scopes as string;

                if (string.IsNullOrEmpty(scope))
                {
                    throw new ArgumentException(
                        "For Managed Identity, scopes must be a single scope string.",
                        nameof(scopes));
                }

                var requestBuilder = app.AcquireTokenForManagedIdentity(scope);

                if (useMtlsPop)
                {
                    requestBuilder = requestBuilder.WithMtlsProofOfPossession();
                }

                result = await requestBuilder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var app = (IConfidentialClientApplication)_app;
                var scopeArray = scopes as string[];

                if (scopeArray == null || scopeArray.Length == 0)
                {
                    throw new ArgumentException(
                        "For Confidential Client, scopes must be a string array with at least one scope.",
                        nameof(scopes));
                }

                var requestBuilder = app.AcquireTokenForClient(scopeArray);

                if (useMtlsPop)
                {
                    requestBuilder = requestBuilder.WithMtlsProofOfPossession();
                }

                result = await requestBuilder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // Validate token type
            if (useMtlsPop)
            {
                if (result.TokenType != Constants.MtlsPoPTokenType)
                {
                    throw new InvalidOperationException(
                        $"Expected token type '{Constants.MtlsPoPTokenType}' but got '{result.TokenType}'. " +
                        "Ensure .WithMtlsProofOfPossession() was used and Leg 1 provided a valid certificate binding.");
                }

                if (result.BindingCertificate == null)
                {
                    throw new InvalidOperationException(
                        "BindingCertificate is null in Leg 2 result. " +
                        "This should not happen with mTLS PoP tokens. " +
                        "Ensure Leg 1 used .WithMtlsProofOfPossession() and ClientSignedAssertion includes TokenBindingCertificate.");
                }
            }
            else
            {
                // Bearer token expected
                if (result.TokenType != "Bearer")
                {
                    throw new InvalidOperationException(
                        $"Expected Bearer token but got '{result.TokenType}'.");
                }
            }

            return result;
        }

        /// <summary>
        /// Exchanges Leg 1 assertion for final token with optional tenant override.
        /// </summary>
        /// <param name="scopes">Target scopes for the final token.</param>
        /// <param name="useMtlsPop">If true, acquires mTLS PoP token; if false, acquires Bearer token.</param>
        /// <param name="tenantId">Optional tenant ID to override the default tenant.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication result with the final token.</returns>
        public async Task<AuthenticationResult> ExchangeTokenAsync(
            object scopes,
            bool useMtlsPop,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(scopes);
            ArgumentNullException.ThrowIfNull(tenantId);

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_app;
                var scope = scopes as string;

                if (string.IsNullOrEmpty(scope))
                {
                    throw new ArgumentException(
                        "For Managed Identity, scopes must be a single scope string.",
                        nameof(scopes));
                }

                var requestBuilder = app
                    .AcquireTokenForManagedIdentity(scope)
                    .WithTenantId(tenantId);

                if (useMtlsPop)
                {
                    requestBuilder = requestBuilder.WithMtlsProofOfPossession();
                }

                return await requestBuilder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var app = (IConfidentialClientApplication)_app;
                var scopeArray = scopes as string[];

                if (scopeArray == null || scopeArray.Length == 0)
                {
                    throw new ArgumentException(
                        "For Confidential Client, scopes must be a string array with at least one scope.",
                        nameof(scopes));
                }

                var requestBuilder = app
                    .AcquireTokenForClient(scopeArray)
                    .WithTenantId(tenantId);

                if (useMtlsPop)
                {
                    requestBuilder = requestBuilder.WithMtlsProofOfPossession();
                }

                return await requestBuilder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the underlying application if it's disposable.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Note: IManagedIdentityApplication and IConfidentialClientApplication
            // don't implement IDisposable in MSAL.NET 4.x, so no cleanup needed.
            _disposed = true;
        }
    }
}
