// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Capability interface for credentials that can provide a binding certificate
    /// for mTLS transport setup (cache key, authority endpoint, TLS channel).
    /// <para>
    /// Unlike assertion generation, certificate discovery is a lightweight operation
    /// that can be performed early in the request pipeline — before cache lookup and
    /// authority resolution — without generating the full client assertion JWT.
    /// </para>
    /// <para>
    /// Implementations may cache the certificate across calls for performance.
    /// The certificate is expected to be stable for a given application configuration;
    /// if it rotates, the next <see cref="IClientCredential.GetCredentialMaterialAsync"/>
    /// call will surface the new certificate and update the cache.
    /// </para>
    /// </summary>
    internal interface IMtlsBindingCertificateProvider
    {
        /// <summary>
        /// Returns the X.509 certificate used for mTLS transport binding.
        /// May return <c>null</c> if the credential does not provide a binding certificate.
        /// </summary>
        Task<X509Certificate2> GetBindingCertificateAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken);
    }
}
