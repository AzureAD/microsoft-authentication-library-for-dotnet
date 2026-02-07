// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPop.Fic
{
    /// <summary>
    /// Helper for creating ClientSignedAssertion from Leg 1 authentication result.
    /// Supports optional certificate binding for jwt-pop scenarios.
    /// </summary>
    /// <remarks>
    /// Production-ready implementation following MSAL.NET conventions:
    /// - Input validation with ArgumentNullException.ThrowIfNull
    /// - Proper certificate handling for PoP scenarios
    /// </remarks>
    public sealed class FicAssertionProvider
    {
        private readonly AuthenticationResult _leg1Result;

        /// <summary>
        /// Creates an assertion provider from Leg 1 authentication result.
        /// </summary>
        /// <param name="leg1Result">Leg 1 authentication result containing the assertion token.</param>
        /// <exception cref="ArgumentNullException">Thrown when leg1Result is null.</exception>
        public FicAssertionProvider(AuthenticationResult leg1Result)
        {
            ArgumentNullException.ThrowIfNull(leg1Result);

            if (string.IsNullOrEmpty(leg1Result.AccessToken))
            {
                throw new ArgumentException(
                    "Leg 1 authentication result does not contain an access token.",
                    nameof(leg1Result));
            }

            _leg1Result = leg1Result;
        }

        /// <summary>
        /// Creates a ClientSignedAssertion for use in Leg 2 token exchange.
        /// </summary>
        /// <param name="bindCertificate">
        /// If true, binds Leg 1's BindingCertificate to the assertion for jwt-pop.
        /// Use true when Leg 2 will request mTLS PoP token, false for Bearer token.
        /// </param>
        /// <returns>ClientSignedAssertion containing Leg 1's token and optional certificate binding.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when bindCertificate is true but Leg 1's BindingCertificate is null.
        /// </exception>
        public ClientSignedAssertion CreateAssertion(bool bindCertificate = false)
        {
            if (bindCertificate && _leg1Result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "Cannot bind certificate: Leg 1's BindingCertificate is null. " +
                    "Ensure Leg 1 used .WithMtlsProofOfPossession() to acquire a PoP token.");
            }

            return new ClientSignedAssertion
            {
                Assertion = _leg1Result.AccessToken,
                TokenBindingCertificate = bindCertificate ? _leg1Result.BindingCertificate : null
            };
        }

        /// <summary>
        /// Creates an assertion provider callback for use with ConfidentialClientApplicationBuilder.WithClientAssertion().
        /// </summary>
        /// <param name="bindCertificate">
        /// If true, binds Leg 1's BindingCertificate to the assertion for jwt-pop.
        /// </param>
        /// <returns>
        /// A callback function that returns ClientSignedAssertion for assertion-based authentication.
        /// </returns>
        public Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> CreateAssertionCallback(
            bool bindCertificate = false)
        {
            return (options, ct) => Task.FromResult(CreateAssertion(bindCertificate));
        }
    }
}
