// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Extended authentication operation that supports mTLS POP certificate injection.
    /// When an operation implements this interface, MSAL will inject the mTLS certificate
    /// instead of replacing the operation with MtlsPopAuthenticationOperation.
    /// This enables CDT + mTLS POP composition where a single operation handles both concerns.
    /// </summary>
    public interface IAuthenticationOperation3 : IAuthenticationOperation2
    {
        /// <summary>
        /// MSAL sets this when WithMtlsProofOfPossession() was called.
        /// The operation can use this certificate for signing, key binding,
        /// and setting BindingCertificate on the AuthenticationResult.
        /// </summary>
        X509Certificate2 MtlsCertificate { set; }
    }
}
