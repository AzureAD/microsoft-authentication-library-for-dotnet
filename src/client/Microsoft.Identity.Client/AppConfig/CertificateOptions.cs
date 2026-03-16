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
    }
}
