// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Encapsulates certificate-related configuration options for confidential client applications.
    /// This class configures the certificate itself and how it's used for authentication,
    /// but not the token acquisition strategy (mTLS bearer vs PoP) which is set at request time.
    /// See https://aka.ms/msal-net-certificate-configuration for details.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class CertificateConfiguration
    {
        /// <summary>
        /// Creates a new instance of <see cref="CertificateConfiguration"/> with the specified certificate.
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <exception cref="ArgumentNullException">Thrown when certificate is null.</exception>
        public CertificateConfiguration(X509Certificate2 certificate)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        /// <summary>
        /// Creates a new instance of <see cref="CertificateConfiguration"/> with a certificate provider.
        /// The provider function will be called each time a certificate is needed, enabling certificate rotation scenarios.
        /// </summary>
        /// <param name="certificateProvider">A function that returns the X509 certificate to use for authentication.</param>
        /// <exception cref="ArgumentNullException">Thrown when certificateProvider is null.</exception>
        public CertificateConfiguration(Func<X509Certificate2> certificateProvider)
        {
            CertificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        }

        /// <summary>
        /// Gets the X509 certificate used for authentication.
        /// This will be null if a certificate provider was specified instead.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets the certificate provider function that returns the certificate to use.
        /// This will be null if a static certificate was specified instead.
        /// Useful for certificate rotation scenarios where the certificate may change over time.
        /// </summary>
        public Func<X509Certificate2> CertificateProvider { get; }

        /// <summary>
        /// Gets or sets whether to send the X5C (certificate chain) with each request.
        /// Applicable to first-party applications only. Sending the x5c enables application developers to achieve 
        /// easy certificate roll-over in Azure AD. See https://aka.ms/msal-net-sni for details.
        /// Default is false.
        /// </summary>
        public bool SendX5C { get; set; }

        /// <summary>
        /// Gets or sets custom claims to be signed by the certificate and included in the client assertion JWT.
        /// These claims are used during application authentication (client credentials flow) and are part of 
        /// the signed JWT that proves the application's identity.
        /// See https://aka.ms/msal-net-client-assertion for details.
        /// </summary>
        /// <remarks>
        /// This is different from <see cref="Claims"/> which are sent as request parameters for Conditional Access.
        /// ClaimsToSign are included in the signed client assertion JWT itself.
        /// </remarks>
        public IDictionary<string, string> ClaimsToSign { get; set; }

        /// <summary>
        /// Gets or sets whether to merge custom claims with the default required claims for authentication.
        /// Only applicable when <see cref="ClaimsToSign"/> is specified.
        /// Default is true. If set to false, you must provide all required default claims.
        /// </summary>
        public bool MergeWithDefaultClaims { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to associate tokens in the cache with this specific certificate and its claims.
        /// When true, tokens acquired with this certificate configuration will be partitioned in the cache
        /// based on a hash of the certificate's public key and the claims being signed.
        /// This ensures tokens acquired with different certificates or different custom claims are cached separately.
        /// Default is false.
        /// </summary>
        /// <remarks>
        /// This is useful in multi-certificate scenarios or when using different custom claims per token request.
        /// The cache key includes both the certificate thumbprint and a hash of the claims to ensure proper isolation.
        /// </remarks>
        public bool AssociateTokensWithCertificate { get; set; }

        /// <summary>
        /// Gets or sets claims to be included in the token request.
        /// These are claims sent as request parameters, typically from Conditional Access claims challenges.
        /// When a token request fails with a claims challenge (e.g., from Conditional Access policies),
        /// retry the acquisition with these claims from the exception.
        /// See https://aka.ms/msal-net-claim-challenge for details.
        /// </summary>
        /// <remarks>
        /// This is different from <see cref="ClaimsToSign"/> which are claims included in the client assertion JWT.
        /// These Claims are sent as the "claims" parameter in the OAuth token request to satisfy policy requirements.
        /// </remarks>
        public string Claims { get; set; }
    }
}
