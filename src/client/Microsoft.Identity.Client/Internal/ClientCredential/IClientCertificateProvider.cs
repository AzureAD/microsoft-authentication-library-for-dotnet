// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Adapter interface for providing client certificates from various sources.
    /// </summary>
    /// <remarks>
    /// This interface enables the adapter pattern to unify certificate resolution
    /// from different sources (IClientCredential implementations, managed identity sources, etc.).
    /// Implementations return a <see cref="ClientCertificateContext"/> that includes both the certificate
    /// and its intended usage (Assertion for JWT signing vs MtlsBinding for TLS binding).
    /// </remarks>
    internal interface IClientCertificateProvider
    {
        /// <summary>
        /// Gets the client certificate context asynchronously, including the certificate and its usage type.
        /// </summary>
        /// <param name="options">
        /// Options for the certificate request, including client ID, token endpoint, and whether MTLS PoP is requested.
        /// The IsMtlsPopRequested flag helps determine the certificate usage type.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>
        /// A <see cref="ClientCertificateContext"/> containing the certificate and its usage type,
        /// or null if this provider does not supply certificates.
        /// The usage type is determined by:
        /// - TokenBindingCertificate → always <see cref="ClientCertificateUsage.MtlsBinding"/>
        /// - Regular certificate → <see cref="ClientCertificateUsage.MtlsBinding"/> if MTLS PoP requested,
        ///   otherwise <see cref="ClientCertificateUsage.Assertion"/>
        /// </returns>
        Task<ClientCertificateContext> GetCertificateAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken);
    }
}
