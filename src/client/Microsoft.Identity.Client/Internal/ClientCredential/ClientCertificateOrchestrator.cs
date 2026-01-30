// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Orchestrates client certificate resolution and validation for confidential client authentication.
    /// </summary>
    /// <remarks>
    /// This orchestrator encapsulates the complexity of resolving certificates from various credential types
    /// and applies appropriate validation based on the authentication scenario (JWT signing vs MTLS PoP).
    /// It handles both certificate-based credentials (WithCertificate) and assertion-based credentials
    /// (WithClientAssertion) that may optionally provide a TokenBindingCertificate.
    /// </remarks>
    internal static class ClientCertificateOrchestrator
    {
        /// <summary>
        /// Resolves and validates a client certificate for authentication.
        /// </summary>
        /// <param name="credential">The client credential which may or may not support certificate provision.</param>
        /// <param name="serviceBundle">Service bundle containing configuration and authority information.</param>
        /// <param name="isMtlsPopRequested">Flag indicating if MTLS Proof-of-Possession is requested.</param>
        /// <param name="claims">Optional claims to include in the certificate request.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>
        /// A <see cref="ClientCertificateContext"/> containing the resolved certificate and its intended usage.
        /// The certificate may be null if the credential doesn't support certificates and MTLS is not requested.
        /// </returns>
        /// <exception cref="MsalClientException">
        /// Thrown if:
        /// - MTLS PoP is requested but the credential type does not support certificates
        /// - The certificate provider returns null when MTLS is requested
        /// - MTLS PoP is requested for AAD without regional endpoint configuration
        /// </exception>
        public static async Task<ClientCertificateContext> ResolveCertificateAsync(
            IClientCredential credential,
            IServiceBundle serviceBundle,
            bool isMtlsPopRequested,
            string claims,
            CancellationToken cancellationToken)
        {
            ClientCertificateContext certContext = null;

            // Check if credential supports certificate provision
            if (credential is IClientCertificateProvider certProvider)
            {
                // Build options for certificate resolution
                var options = new AssertionRequestOptions
                {
                    ClientID = serviceBundle.Config.ClientId,
                    Claims = claims,
                    ClientCapabilities = serviceBundle.Config.ClientCapabilities,
                    IsMtlsPopRequested = isMtlsPopRequested,  // Pass flag to provider
                    CancellationToken = cancellationToken
                };

                // Resolve the certificate context from the provider
                // Provider now returns context with usage already determined
                certContext = await certProvider
                    .GetCertificateAsync(options, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Validate if MTLS PoP is requested
            if (isMtlsPopRequested)
            {
                // Check 1: Certificate must be provided (before checking region)
                if (certContext?.Certificate == null)
                {
                    // Credential doesn't support certs or provider returned null
                    string credentialTypeName = credential.GetType().Name;
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        $"MTLS Proof-of-Possession requires a certificate. " +
                        $"The credential type '{credentialTypeName}' does not support certificates or did not provide one. " +
                        $"Use WithCertificate() to configure a certificate, or use WithClientAssertion() with a TokenBindingCertificate.");
                }

                // Check 2: Validate certificate for MTLS (region check happens here)
                ClientCertificateValidation.ValidateForMtlsPoP(certContext.Certificate, serviceBundle);
            }

            // Return the context as-is (usage already set by provider)
            return certContext;
        }
    }
}
