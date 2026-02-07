// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Builds ClientSignedAssertion for FIC two-leg token exchange from Leg 1 result.
    /// </summary>
    /// <remarks>
    /// This helper constructs the assertion required for Leg 2 of the FIC flow,
    /// optionally including the binding certificate for mTLS PoP final tokens.
    /// </remarks>
    public sealed class FicAssertionProvider
    {
        private readonly AuthenticationResult _leg1Result;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicAssertionProvider"/> class.
        /// </summary>
        /// <param name="leg1Result">The authentication result from Leg 1.</param>
        /// <exception cref="ArgumentNullException">Thrown when leg1Result is null.</exception>
        /// <exception cref="ArgumentException">Thrown when leg1Result.AccessToken is null or empty.</exception>
        public FicAssertionProvider(AuthenticationResult leg1Result)
        {
            ArgumentNullException.ThrowIfNull(leg1Result);

            if (string.IsNullOrEmpty(leg1Result.AccessToken))
            {
                throw new ArgumentException(
                    "Leg 1 AuthenticationResult must have a non-empty AccessToken.",
                    nameof(leg1Result));
            }

            _leg1Result = leg1Result;
        }

        /// <summary>
        /// Creates a ClientSignedAssertion for Leg 2 token exchange.
        /// </summary>
        /// <param name="includeBindingCertificate">
        /// When true, includes Leg 1's BindingCertificate for mTLS PoP final token.
        /// When false, creates assertion for Bearer final token.
        /// </param>
        /// <returns>A ClientSignedAssertion configured for the specified token type.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when includeBindingCertificate is true but Leg 1 has no BindingCertificate.
        /// </exception>
        public ClientSignedAssertion CreateAssertion(bool includeBindingCertificate = false)
        {
            if (includeBindingCertificate && _leg1Result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "Cannot include BindingCertificate in assertion: Leg 1 result does not have a BindingCertificate. " +
                    "Ensure Leg 1 was acquired with .WithMtlsProofOfPossession().");
            }

            return new ClientSignedAssertion
            {
                Assertion = _leg1Result.AccessToken,
                TokenBindingCertificate = includeBindingCertificate ? _leg1Result.BindingCertificate : null
            };
        }

        /// <summary>
        /// Creates a ClientSignedAssertion for Bearer token final result (no certificate binding).
        /// </summary>
        /// <returns>A ClientSignedAssertion for Bearer token.</returns>
        public ClientSignedAssertion CreateBearerAssertion()
        {
            return CreateAssertion(includeBindingCertificate: false);
        }

        /// <summary>
        /// Creates a ClientSignedAssertion for mTLS PoP token final result (with certificate binding).
        /// </summary>
        /// <returns>A ClientSignedAssertion for mTLS PoP token.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when Leg 1 result does not have a BindingCertificate.
        /// </exception>
        public ClientSignedAssertion CreateMtlsPopAssertion()
        {
            return CreateAssertion(includeBindingCertificate: true);
        }

        /// <summary>
        /// Gets the Leg 1 authentication result used to create assertions.
        /// </summary>
        public AuthenticationResult Leg1Result => _leg1Result;
    }
}
