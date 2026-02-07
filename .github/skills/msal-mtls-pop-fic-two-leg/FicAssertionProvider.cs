// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Client.Helpers
{
    /// <summary>
    /// Helper class for building ClientSignedAssertion from Leg 1 authentication results.
    /// Used in FIC two-leg token exchange flows.
    /// </summary>
    public class FicAssertionProvider
    {
        private readonly string _leg1Token;
        private readonly System.Security.Cryptography.X509Certificates.X509Certificate2 _leg1Certificate;

        /// <summary>
        /// Initializes a new instance with Leg 1 authentication result.
        /// </summary>
        /// <param name="leg1Result">The authentication result from Leg 1.</param>
        /// <exception cref="ArgumentNullException">If leg1Result is null.</exception>
        /// <exception cref="InvalidOperationException">If leg1Result is missing token or certificate.</exception>
        public FicAssertionProvider(AuthenticationResult leg1Result)
        {
            ArgumentNullException.ThrowIfNull(leg1Result);

            if (string.IsNullOrEmpty(leg1Result.AccessToken))
            {
                throw new InvalidOperationException(
                    "Leg 1 authentication result must contain an access token.");
            }

            if (leg1Result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "Leg 1 authentication result must contain a BindingCertificate. " +
                    "Ensure Leg 1 used .WithMtlsProofOfPossession().");
            }

            _leg1Token = leg1Result.AccessToken;
            _leg1Certificate = leg1Result.BindingCertificate;
        }

        /// <summary>
        /// Gets the ClientSignedAssertion for use in Leg 2.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ClientSignedAssertion containing Leg 1 token and certificate binding.</returns>
        public Task<ClientSignedAssertion> GetAssertionAsync(CancellationToken cancellationToken = default)
        {
            var assertion = new ClientSignedAssertion
            {
                // The Leg 1 access token (forwarded as client_assertion)
                Assertion = _leg1Token,

                // The Leg 1 binding certificate (enables jwt-pop client_assertion_type)
                TokenBindingCertificate = _leg1Certificate
            };

            return Task.FromResult(assertion);
        }

        /// <summary>
        /// Creates a callback function for use with WithClientAssertion().
        /// </summary>
        /// <returns>A function that returns the ClientSignedAssertion.</returns>
        public Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> GetAssertionCallback()
        {
            return (options, ct) => GetAssertionAsync(ct);
        }
    }
}
