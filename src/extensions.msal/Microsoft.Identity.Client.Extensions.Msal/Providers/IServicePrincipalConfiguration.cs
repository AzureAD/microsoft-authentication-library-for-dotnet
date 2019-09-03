// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// IManagedIdentityConfiguration provides the configurable properties for the ManagedIdentityProbe
    /// </summary>
    internal interface IServicePrincipalConfiguration
    {
        /// <summary>
        /// CertificateBase64 is the base64 encoded representation of an x509 certificate
        /// </summary>
        string CertificateBase64 { get; }

        /// <summary>
        /// CertificateThumbprint is the thumbprint of the certificate in the Windows Certificate Store
        /// </summary>
        string CertificateThumbprint { get; }

        /// <summary>
        /// CertificateSubjectDistinguishedName is the subject distinguished name of the certificate in the Windows Certificate Store
        /// </summary>
        string CertificateSubjectDistinguishedName { get; }

        /// <summary>
        /// CertificateStoreName is the name of the certificate store on Windows where the certificate is stored
        /// </summary>
        string CertificateStoreName { get; }

        /// <summary>
        /// CertificateStoreLocation is the location of the certificate store on Windows where the certificate is stored
        /// </summary>
        string CertificateStoreLocation { get; }

        /// <summary>
        /// TenantId is the AAD TenantID
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// ClientId is the service principal (application) ID
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// ClientSecret is the service principal (application) string secret
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// Authority is the URI pointing to the AAD endpoint
        /// </summary>
        string Authority { get; }
    }
}
