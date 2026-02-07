// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Production helper for creating ClientSignedAssertion with certificate binding for FIC two-leg flows.
    /// Encapsulates assertion and TokenBindingCertificate to enable jwt-pop client assertion type.
    /// </summary>
    /// <remarks>
    /// Based on test code: ClientCredentialsMtlsPopTests.Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync
    /// </remarks>
    public class FicAssertionProvider
    {
        private readonly string _assertionJwt;
        private readonly X509Certificate2 _bindingCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicAssertionProvider"/> class.
        /// </summary>
        /// <param name="assertionJwt">JWT token from Leg 1 to use as client assertion in Leg 2.</param>
        /// <param name="bindingCertificate">X.509 certificate to bind the assertion (enables jwt-pop).</param>
        public FicAssertionProvider(string assertionJwt, X509Certificate2 bindingCertificate)
        {
            if (string.IsNullOrEmpty(assertionJwt))
                throw new ArgumentException("Assertion JWT cannot be null or empty.", nameof(assertionJwt));

            _assertionJwt = assertionJwt;
            _bindingCertificate = bindingCertificate ?? throw new ArgumentNullException(nameof(bindingCertificate));
        }

        /// <summary>
        /// Returns a ClientSignedAssertion for use in MSAL's WithClientAssertion callback.
        /// </summary>
        /// <param name="tokenEndpoint">Token endpoint provided by MSAL (from AssertionRequestOptions).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ClientSignedAssertion with assertion JWT and binding certificate.</returns>
        /// <remarks>
        /// When used with WithMtlsProofOfPossession(), MSAL automatically sets:
        /// client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-pop"
        /// </remarks>
        public Task<ClientSignedAssertion> GetAssertionAsync(
            string tokenEndpoint,
            CancellationToken cancellationToken = default)
        {
            // Optional: Log or validate tokenEndpoint if needed
            if (string.IsNullOrEmpty(tokenEndpoint))
            {
                throw new ArgumentException("Token endpoint cannot be null or empty.", nameof(tokenEndpoint));
            }

            return Task.FromResult(new ClientSignedAssertion
            {
                Assertion = _assertionJwt,                        // Forwarded as client_assertion
                TokenBindingCertificate = _bindingCertificate     // Binds assertion for jwt-pop
            });
        }

        /// <summary>
        /// Creates an assertion provider callback for use with ConfidentialClientApplicationBuilder.WithClientAssertion().
        /// </summary>
        /// <returns>A callback function compatible with WithClientAssertion().</returns>
        public Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> GetCallback()
        {
            return (options, ct) => GetAssertionAsync(options.TokenEndpoint, ct);
        }
    }
}
