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
        /// The client certificate resolved by the selected credential, if any.
        /// In regular certificate-auth flows this is the certificate used by the credential.
        /// In mTLS / bound-credential flows this is the certificate attached to transport.
        /// Null for secret-based and plain string-assertion credentials.
        /// Optional certificate returned by the credential.
        /// Present when:
        /// <list type="bullet">
        ///   <item><description>A certificate credential was used and its certificate was resolved.</description></item>
        ///   <item><description>A delegate credential returned a <see cref="ClientSignedAssertion"/> with a <see cref="ClientSignedAssertion.TokenBindingCertificate"/>.</description></item>
        /// </list>
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
                ?? throw new InvalidOperationException("TokenRequestParameters must not be null.");
            ResolvedCertificate = resolvedCertificate;
        }
    }
}
