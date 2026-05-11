// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Runtime context that MSAL hands to an <see cref="IAuthenticationOperation3"/>
    /// implementation once per token request, after MSAL has evaluated the credentials
    /// that will be used for the request. The operation reads the properties it needs
    /// and prepares to format the result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations must not retain a reference to the context past the
    /// <see cref="IAuthenticationOperation3.AfterCredentialEvaluationAsync"/> call.
    /// </para>
    /// <para>
    /// New properties may be added to this class in future MSAL versions; existing
    /// implementations remain source- and binary-compatible because the type is
    /// sealed and consumed only by property access.
    /// </para>
    /// <para>
    /// <b>KeyId contract:</b> Implementations that receive an mTLS certificate via
    /// <see cref="MtlsCertificate"/> must incorporate the certificate's identity
    /// (e.g., thumbprint) into <see cref="IAuthenticationOperation.KeyId"/>.
    /// MSAL uses <c>KeyId</c> as part of the token cache key — a hardcoded or
    /// cert-independent <c>KeyId</c> will cause stale tokens to be served after
    /// certificate rotation.
    /// </para>
    /// </remarks>
    public sealed class TokenAcquisitionContext
    {
        /// <summary>
        /// The mTLS certificate MSAL will use on the wire for the current token request.
        /// Refreshed per request — operations must not cache this value across requests.
        /// </summary>
        public X509Certificate2 MtlsCertificate { get; init; }
    }
}
