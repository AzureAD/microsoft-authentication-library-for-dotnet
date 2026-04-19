// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{

    /// <summary>
    /// Normalized output of <see cref="IClientCredential.GetCredentialMaterialAsync"/>.
    /// Replaces the former <c>ClientCredentialApplicationResult</c> and decouples "what credentials
    /// produce" from "how the token client applies them".
    /// </summary>
    internal sealed class CredentialMaterial
    {
        /// <summary>
        /// Key/value pairs to be added to the token-request body.
        /// Never <see langword="null"/>; may be empty (e.g., for pure mTLS-transport mode where the
        /// certificate authenticates the client at the TLS layer and no assertion is needed).
        /// </summary>
        public IReadOnlyDictionary<string, string> TokenRequestParameters { get; }

        /// <summary>
        /// Optional certificate returned by the credential.
        /// Present when a certificate credential was used (regular or mTLS) or a delegate credential
        /// returned a <see cref="ClientSignedAssertion"/> with a
        /// <see cref="ClientSignedAssertion.TokenBindingCertificate"/>.
        /// <see langword="null"/> when no certificate is involved (secret, plain JWT assertion).
        /// </summary>
        public X509Certificate2 ResolvedCertificate { get; }

        /// <param name="tokenRequestParameters">Body parameters to add to the token request. Must not be null.</param>
        /// <param name="resolvedCertificate">Optional certificate for mTLS transport or logging.</param>
        public CredentialMaterial(
            IReadOnlyDictionary<string, string> tokenRequestParameters,
            X509Certificate2 resolvedCertificate = null)
        {
            TokenRequestParameters = tokenRequestParameters
                ?? throw new ArgumentNullException(nameof(tokenRequestParameters));
            ResolvedCertificate = resolvedCertificate;
        }
    }
}
