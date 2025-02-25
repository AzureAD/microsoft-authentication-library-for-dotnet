// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.RP
{
    /// <summary>
    /// Resource Provider extensibility methods for <see cref="ConfidentialClientApplicationBuilder"/>
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public static class ConfidentialClientApplicationBuilderForResourceProviders
    {
        /// <summary>
        /// Sets the certificate associated with the application.
        /// Applicable to first-party applications only, this method also allows to specify 
        /// if the <see href="https://datatracker.ietf.org/doc/html/rfc7517#section-4.7">x5c claim</see> should be sent to Azure AD.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the certificate chain to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// This api allow you to associate the tokens acquired from Azure AD with the certificate serial number. 
        /// This can be used to partition the cache by certificate. Tokens acquired with one certificate will not be available to another certificate with a different serial number.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <param name="sendX5C">To send X5C with every request or not. The default is <c>false</c></param>
        /// <param name="associateTokensWithCertificateSerialNumber">Determines if the application tokens acquired from Azure AD are associated with the certificate serial number</param>
        /// <remarks>You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys. </remarks>
        public static ConfidentialClientApplicationBuilder WithCertificate(
            this ConfidentialClientApplicationBuilder builder,
            X509Certificate2 certificate, bool sendX5C, bool associateTokensWithCertificateSerialNumber)
        {
            builder.WithCertificate(certificate, sendX5C);

            if (associateTokensWithCertificateSerialNumber)
            {
                builder.Config.CertificateIdToAssociateWithToken = certificate.SerialNumber;
            }

            return builder;
        }
    }
}
