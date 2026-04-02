// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// Implemented by authentication operations that carry an mTLS binding certificate
    /// (e.g. <see cref="MtlsPopAuthenticationOperation"/>).
    /// Allows the rest of the pipeline to extract the transport cert from the auth scheme
    /// instead of passing it separately.
    /// </summary>
    internal interface IMtlsCertificateProvider
    {
        X509Certificate2 Certificate { get; }
    }
}
