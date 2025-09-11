// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Container returned from <c>WithClientAssertion</c>.
    /// </summary>
    public class ClientSignedAssertion
    {
        /// <summary>
        /// Represents the client assertion (JWT) and optional mutual‑TLS binding certificate returned
        /// by the <c>clientAssertionProvider</c> callback supplied to
        /// <see cref="ConfidentialClientApplicationBuilder.WithClientAssertion(System.Func{AssertionRequestOptions, System.Threading.CancellationToken, System.Threading.Tasks.Task{ClientSignedAssertion}})"/>.
        /// </summary>
        /// <remarks>
        /// MSAL forwards <see cref="Assertion"/> to the token endpoint as the <c>client_assertion</c> parameter.
        /// When mutual‑TLS Proof‑of‑Possession (PoP) is enabled on the application and a
        /// <see cref="TokenBindingCertificate"/> is provided, MSAL sets <c>client_assertion_type</c> to
        /// <c>urn:ietf:params:oauth:client-assertion-type:jwt-pop</c>; otherwise it uses <c>jwt-bearer</c>.
        /// <br/><br/>
        /// Guidance on constructing the client assertion (required claims, audience, and lifetime) is available at
        /// <see href="https://aka.ms/msal-net-client-assertion">aka.ms/msal-net-client-assertion</see>.
        /// The assertion is created by your callback; MSAL does not modify or re‑sign it.
        /// **Note:** It is up to the caller to cache the assertion and certificate if reuse is desired.
        /// </remarks>
        public string Assertion { get; set; }

        /// <summary>
        /// Optional. Certificate used to bind the client assertion for mutual‑TLS Proof‑of‑Possession (PoP).
        /// </summary>
        /// <remarks>
        /// Provide a value only when PoP is enabled on the application. The certificate should include an
        /// accessible private key. If <c>null</c>, MSAL treats the assertion as a bearer assertion and uses
        /// <c>client_assertion_type=jwt-bearer</c>.
        /// </remarks>
        public X509Certificate2 TokenBindingCertificate { get; set; }
    }
}
