// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPop.Fic
{
    /// <summary>
    /// Helper for executing Leg 2 token exchange in FIC two-leg flow.
    /// MUST use Confidential Client (MSI does not have WithClientAssertion API).
    /// Supports both Bearer and mTLS PoP final tokens.
    /// </summary>
    /// <remarks>
    /// Production-ready implementation following MSAL.NET conventions:
    /// - ConfigureAwait(false) on all awaits
    /// - CancellationToken support with defaults
    /// - Proper IDisposable implementation
    /// - Input validation and disposal checks
    /// 
    /// IMPORTANT: Only IConfidentialClientApplication can perform Leg 2 exchange.
    /// IManagedIdentityApplication does NOT have WithClientAssertion() method.
    /// </remarks>
    public sealed class FicLeg2Exchanger : IDisposable
    {
        private readonly IConfidentialClientApplication _confApp;
        private readonly FicAssertionProvider _assertionProvider;
        private bool _disposed;

        /// <summary>
        /// Creates a Leg 2 exchanger for Confidential Client.
        /// </summary>
        /// <param name="confApp">
        /// Configured Confidential Client application with WithClientAssertion callback set.
        /// This MUST be a Confidential Client - MSI cannot perform Leg 2 exchange.
        /// </param>
        /// <param name="assertionProvider">Provider that creates ClientSignedAssertion from Leg 1 result.</param>
        /// <exception cref="ArgumentNullException">Thrown when confApp or assertionProvider is null.</exception>
        public FicLeg2Exchanger(
            IConfidentialClientApplication confApp,
            FicAssertionProvider assertionProvider)
        {
            ArgumentNullException.ThrowIfNull(confApp);
            ArgumentNullException.ThrowIfNull(assertionProvider);
            
            _confApp = confApp;
            _assertionProvider = assertionProvider;
        }

        /// <summary>
        /// Exchanges the Leg 1 token for a final target resource token.
        /// </summary>
        /// <param name="scopes">
        /// Target resource scopes (e.g., "https://graph.microsoft.com/.default").
        /// Must include ".default" suffix.
        /// </param>
        /// <param name="requestMtlsPop">
        /// If true, requests mTLS PoP token and binds with Leg 1's certificate.
        /// If false, requests standard Bearer token.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// Authentication result containing the final token (Bearer or mTLS PoP).
        /// If mTLS PoP was requested, BindingCertificate will match Leg 1's certificate.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when scopes is null.</exception>
        /// <exception cref="ArgumentException">Thrown when scopes array is empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the exchanger has been disposed.</exception>
        /// <exception cref="MsalException">Thrown when token exchange fails.</exception>
        public async Task<AuthenticationResult> ExchangeTokenAsync(
            string[] scopes,
            bool requestMtlsPop = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scopes);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (scopes.Length == 0)
            {
                throw new ArgumentException("Scopes array cannot be empty.", nameof(scopes));
            }

            // Validate scopes format
            if (!scopes.Any(s => s.EndsWith("/.default", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    "Scopes must include '.default' suffix (e.g., 'https://graph.microsoft.com/.default').",
                    nameof(scopes));
            }

            var requestBuilder = _confApp.AcquireTokenForClient(scopes);

            if (requestMtlsPop)
            {
                // Request mTLS PoP token - certificate binding happens automatically
                // via ClientSignedAssertion.TokenBindingCertificate
                requestBuilder = requestBuilder.WithMtlsProofOfPossession();
            }

            var result = await requestBuilder
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Validate result based on request type
            if (requestMtlsPop)
            {
                if (result.TokenType != "mtls_pop")
                {
                    throw new InvalidOperationException(
                        $"Expected token type 'mtls_pop' but got '{result.TokenType}'. " +
                        "Verify that .WithMtlsProofOfPossession() was called correctly.");
                }

                if (result.BindingCertificate == null)
                {
                    throw new InvalidOperationException(
                        "BindingCertificate is null after mTLS PoP token acquisition. " +
                        "Verify that Leg 1's BindingCertificate was passed correctly in ClientSignedAssertion.");
                }
            }

            return result;
        }

        /// <summary>
        /// Disposes the exchanger. Note that the underlying Confidential Client application
        /// instance is NOT disposed, as it may be reused elsewhere.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // Note: We don't dispose _confApp as it may be reused
            // Caller is responsible for disposing it when appropriate
            _disposed = true;
        }
    }
}
