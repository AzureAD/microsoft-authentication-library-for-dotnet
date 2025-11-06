// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Encapsulates all certificate-related configuration options for confidential client applications.
    /// This class provides a unified way to configure certificates for authentication and mTLS scenarios.
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
        /// Gets the X509 certificate used for authentication.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets or sets whether to send the X5C (certificate chain) with each request.
        /// Applicable to first-party applications only. Sending the x5c enables application developers to achieve 
        /// easy certificate roll-over in Azure AD. See https://aka.ms/msal-net-sni for details.
        /// Default is false.
        /// </summary>
        public bool SendX5C { get; set; }

        /// <summary>
        /// Gets or sets custom claims to be signed by the certificate.
        /// When specified, these claims will be included in the client assertion.
        /// See https://aka.ms/msal-net-client-assertion for details.
        /// </summary>
        public IDictionary<string, string> ClaimsToSign { get; set; }

        /// <summary>
        /// Gets or sets whether to merge custom claims with the default required claims for authentication.
        /// Only applicable when <see cref="ClaimsToSign"/> is specified.
        /// Default is true. If set to false, you must provide all required default claims.
        /// </summary>
        public bool MergeWithDefaultClaims { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable mTLS Proof-of-Possession for this certificate.
        /// When enabled, tokens will be bound to the certificate using mutual TLS.
        /// This requires an Azure region to be configured and a tenanted authority.
        /// See https://aka.ms/msal-net-pop for details.
        /// Default is false.
        /// </summary>
        public bool EnableMtlsProofOfPossession { get; set; }

        /// <summary>
        /// Gets or sets whether to request a bearer token (instead of a PoP token) when using mTLS.
        /// When true, the certificate is used for mTLS authentication but the token returned is a standard bearer token.
        /// When false (default), a PoP token bound to the certificate is returned.
        /// Only applicable when <see cref="EnableMtlsProofOfPossession"/> is true.
        /// Default is false (returns PoP token).
        /// </summary>
        /// <remarks>
        /// Bearer tokens over mTLS provide authentication at the transport layer while maintaining 
        /// standard token format. PoP tokens provide additional security by binding the token to the certificate.
        /// </remarks>
        public bool UseBearerTokenWithMtls { get; set; }

        /// <summary>
        /// Gets or sets claims to be included in the token request.
        /// This is used for claims challenge scenarios, such as when Conditional Access policies 
        /// require additional claims. When a token request fails with a claims challenge, retry 
        /// the acquisition with the claims value from the exception.
        /// See https://aka.ms/msal-net-claim-challenge for details.
        /// </summary>
        /// <remarks>
        /// This is different from <see cref="ClaimsToSign"/> which are claims included in the client assertion.
        /// These claims are sent as part of the token request to satisfy Conditional Access requirements.
        /// </remarks>
        public string Claims { get; set; }
    }
}
