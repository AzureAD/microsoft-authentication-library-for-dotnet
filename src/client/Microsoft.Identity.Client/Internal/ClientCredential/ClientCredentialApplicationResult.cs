// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Result returned by <see cref="IClientCredential.AddConfidentialClientParametersAsync"/>.
    /// Contains extra data about how the client credential was applied.
    /// Returns the information needed to set JWT-PoP and mTLS transport.
    /// Internal and can be extended in the future to return more data as needed.
    /// </summary>
    internal sealed class ClientCredentialApplicationResult
    {
        /// <summary>
        /// Shared default instance for the common case where the credential has no extra data to return.
        /// </summary>
        public static ClientCredentialApplicationResult None { get; } = new ClientCredentialApplicationResult();

        public ClientCredentialApplicationResult() { }

        public ClientCredentialApplicationResult(
            bool useJwtPopClientAssertion,
            X509Certificate2 mtlsCertificate)
        {
            UseJwtPopClientAssertion = useJwtPopClientAssertion;
            MtlsCertificate = mtlsCertificate;
        }

        /// <summary>
        /// Indicates whether the client_assertion_type was set to JWT-PoP.
        /// </summary>
        internal bool UseJwtPopClientAssertion { get; set; }

        /// <summary>
        /// Optional certificate that should be used for mTLS transport / token binding.
        /// </summary>
        internal X509Certificate2 MtlsCertificate { get; set; }
    }
}
