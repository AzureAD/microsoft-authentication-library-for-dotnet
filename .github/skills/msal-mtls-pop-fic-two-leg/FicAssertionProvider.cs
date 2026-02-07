// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopFicTwoLeg
{
    /// <summary>
    /// Builds ClientSignedAssertion for FIC two-leg flow Leg 2.
    /// Wraps an exchange token (from Leg 1) with certificate binding for jwt-pop.
    /// </summary>
    /// <remarks>
    /// Use this class to create the assertion for Leg 2 of the FIC two-leg flow.
    /// The assertion combines:
    /// - The exchange token from Leg 1 (as the assertion JWT)
    /// - The certificate for token binding (enables jwt-pop client_assertion_type)
    /// </remarks>
    public class FicAssertionProvider
    {
        private readonly string _exchangeToken;
        private readonly X509Certificate2 _bindingCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicAssertionProvider"/> class.
        /// </summary>
        /// <param name="exchangeToken">
        /// The access token from Leg 1 to use as the assertion.
        /// Typically obtained from AcquireTokenForClient with "api://AzureADTokenExchange/.default" scope.
        /// </param>
        /// <param name="bindingCertificate">
        /// The X.509 certificate used for token binding.
        /// Must be the same certificate used in Leg 1.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null or empty.</exception>
        public FicAssertionProvider(string exchangeToken, X509Certificate2 bindingCertificate)
        {
            if (string.IsNullOrWhiteSpace(exchangeToken))
                throw new ArgumentNullException(nameof(exchangeToken));
            if (bindingCertificate == null)
                throw new ArgumentNullException(nameof(bindingCertificate));

            _exchangeToken = exchangeToken;
            _bindingCertificate = bindingCertificate;
        }

        /// <summary>
        /// Gets the ClientSignedAssertion for use in Leg 2.
        /// </summary>
        /// <returns>
        /// A <see cref="ClientSignedAssertion"/> containing:
        /// - Assertion: The exchange token from Leg 1
        /// - TokenBindingCertificate: The certificate for jwt-pop binding
        /// </returns>
        /// <remarks>
        /// When TokenBindingCertificate is set, MSAL will use:
        /// client_assertion_type: urn:ietf:params:oauth:client-assertion-type:jwt-pop
        /// </remarks>
        public ClientSignedAssertion GetAssertion()
        {
            return new ClientSignedAssertion
            {
                Assertion = _exchangeToken,
                TokenBindingCertificate = _bindingCertificate
            };
        }

        /// <summary>
        /// Gets an async callback suitable for WithClientAssertion.
        /// </summary>
        /// <returns>
        /// A function that can be passed to WithClientAssertion() in the app builder.
        /// </returns>
        /// <example>
        /// <code>
        /// var provider = new FicAssertionProvider(leg1Token, cert);
        /// var app = ConfidentialClientApplicationBuilder.Create(clientId)
        ///     .WithClientAssertion(provider.GetAssertionCallback())
        ///     .Build();
        /// </code>
        /// </example>
        public Func<AssertionRequestOptions, System.Threading.CancellationToken, Task<ClientSignedAssertion>> GetAssertionCallback()
        {
            return (options, ct) => Task.FromResult(GetAssertion());
        }
    }
}
