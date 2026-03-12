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
        /// Gets or sets a value indicating whether the certificate should be sent over the mTLS connection 
        /// for token acquisition (without requiring PoP token type).
        /// When <see langword="true"/>, the certificate will be sent in the TLS handshake (client certificate authentication)
        /// while acquiring tokens, which may be standard Bearer tokens or PoP tokens depending on request-level configuration.
        /// When <see langword="false"/> (default), the certificate is sent as a JWT assertion in the request body.
        /// </summary>
        /// <remarks>
        /// <para><b>Default transport</b>:
        /// This property sets the DEFAULT transport for requests that do not explicitly call 
        /// <see cref="AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession()"/>.
        /// Request-level <see cref="AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession()"/> 
        /// always implies mTLS transport, regardless of this setting.
        /// </para>
        /// <para><b>Applicable to certificate credentials only</b>:
        /// Because <see cref="CertificateOptions"/> is accepted exclusively by
        /// <see cref="ConfidentialClientApplicationBuilder.WithCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2, CertificateOptions)"/>
        /// and its overloads, this property is inherently scoped to certificate-based credentials.
        /// As a defensive measure, <see cref="ConfidentialClientApplicationBuilder.Build()"/> will throw
        /// <see cref="MsalClientException"/> with error code <c><see cref="MsalError.InvalidCredentialMaterial"/></c>
        /// if this property is <see langword="true"/> but the configured credential is not certificate-based.
        /// </para>
        /// <para><b>AAD authority region requirement</b>:
        /// When this property is <see langword="true"/> and the authority is an AAD authority (login.microsoftonline.com),
        /// <see cref="ConfidentialClientApplicationBuilder.WithAzureRegion(string)"/> <b>must</b> also be configured.
        /// mTLS Bearer token acquisition is routed to a regional endpoint; without a region, token acquisition will fail
        /// at runtime with error code <c><see cref="MsalError.MtlsBearerWithoutRegion"/></c>.
        /// Non-AAD authorities (ADFS, B2C, etc.) do not require a region.
        /// </para>
        /// <example>
        /// <code>
        /// // mTLS Bearer token — note WithAzureRegion is required for AAD authorities
        /// var options = new CertificateOptions { SendCertificateOverMtls = true };
        /// var app = ConfidentialClientApplicationBuilder
        ///     .Create(clientId)
        ///     .WithAzureRegion("eastus")   // required for AAD when SendCertificateOverMtls = true
        ///     .WithCertificate(cert, options)
        ///     .Build();
        /// 
        /// var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        /// // Result: Bearer token, certificate sent over mTLS
        /// 
        /// // mTLS PoP (also supported with the same app instance)
        /// var result = await app.AcquireTokenForClient(scopes)
        ///     .WithMtlsProofOfPossession()
        ///     .ExecuteAsync();
        /// // Result: PoP token, certificate sent over mTLS
        /// </code>
        /// </example>
        /// </remarks>
        public bool SendCertificateOverMtls { get; init; } = false;
    }
}
