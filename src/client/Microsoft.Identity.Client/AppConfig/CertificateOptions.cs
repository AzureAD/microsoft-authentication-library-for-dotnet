// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Represents configuration options for certificate handling or management.
    /// </summary>
    public record CertificateOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the X.509 certificate chain (x5c) should be included in the token
        /// request.
        /// </summary>
        /// <remarks>Set this property to <see langword="true"/> to include X5C in the token request 
        /// otherwise, set it to <see langword="false"/>.</remarks>
        public bool SendX5C { get; init; } = false;

        /// <summary>
        /// Gets or sets a value indicating if the application tokens acquired from Azure AD are associated with the certificate serial number.
        /// This property when set, allow you to associate the tokens acquired from Azure AD with the certificate serial number. 
        /// This can be used to partition the cache by certificate. Tokens acquired with one certificate will not be accessible to another certificate with a different serial number.
        /// <remarks>Set this property to <see langword="true"/> to indicate that the tokens acquired from Azure AD are associated with the certificate serial number,
        /// by default it is set to <see langword="false"/> /></remarks>
        /// </summary>
        public bool AssociateTokensWithCertificate { get; init; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the certificate should be sent over mTLS
        /// (TLS client certificate authentication) as the default transport for token requests.
        /// When <see langword="true"/>, the certificate is sent in the TLS handshake instead of as a
        /// JWT assertion in the request body. This controls transport only — the resulting token type
        /// depends on request-level configuration: a plain request produces a Bearer token, while
        /// <see cref="AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession"/> produces
        /// an mTLS PoP token.
        /// When <see langword="false"/> (default), the certificate is sent as a JWT assertion in the request body.
        /// </summary>
        /// <remarks>
        /// <para>This property sets the default transport for requests that do not explicitly call
        /// <see cref="AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession"/>.</para>
        /// <para>Request-level <see cref="AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession"/>
        /// always implies mTLS transport, regardless of this setting.</para>
        /// <para>This option is supported with certificate-based credentials configured via
        /// <see cref="ConfidentialClientApplicationBuilder.WithCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2, CertificateOptions)"/>
        /// (static certificate) or the dynamic certificate provider overload.
        /// Non-certificate credentials will throw at build time.</para>
        /// <para>When no Azure region is configured, requests are sent to the global mTLS endpoint.
        /// When <see cref="ConfidentialClientApplicationBuilder.WithAzureRegion(string)"/> is also set,
        /// requests are routed to the regional mTLS endpoint instead.</para>
        /// </remarks>
        public bool SendCertificateOverMtls { get; init; } = false;
    }
}
