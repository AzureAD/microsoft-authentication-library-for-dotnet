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
    /// Implementations must not retain a reference to the context past the
    /// <see cref="IAuthenticationOperation3.AfterCredentialEvaluation"/> call.
    /// New properties may be added to this class in future MSAL versions; existing
    /// implementations remain source- and binary-compatible because the type is
    /// sealed and consumed only by property access.
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
