// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME

    /// <summary>
    /// Certificate for a client assertion. This class is used in one of the constructors of <see cref="ClientCredential"/>. ClientCredential
    /// is itself used in the constructor of <see cref="ConfidentialClientApplication"/> to pass to Azure AD a shared secret (registered in the
    /// Azure AD application)
    /// </summary>
    /// <seealso cref="ClientCredential"/> for the constructor of <seealso cref="ClientCredential"/>
    /// with a certificate, and <seealso cref="ConfidentialClientApplication"/>
    /// <remarks>To understand the difference between public client applications and confidential client applications, see https://aka.ms/msal-net-client-applications</remarks>
    internal sealed class ClientAssertionCertificateWrapper
    {
        /// <summary>
        /// Constructor to create certificate information used in <see cref="ClientCredential"/>
        /// to instantiate a <see cref="ClientCredential"/> used in the constructors of <see cref="ConfidentialClientApplication"/>
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <param name="claimsToSign">Claims to sign with the certificate.</param>
        public ClientAssertionCertificateWrapper(X509Certificate2 certificate, Dictionary<string, string> claimsToSign)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

#if DESKTOP
            if (certificate.PublicKey.Key.KeySize < MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException(nameof(certificate),
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.CertificateKeySizeTooSmallTemplate,
                        MinKeySizeInBits));
            }

            if (claimsToSign != null && claimsToSign.Count > 0)
            {
                ClaimsToSign = claimsToSign;
            }
#endif
        }

        /// <summary>
        /// Constructor to create certificate information used in <see cref="ClientCredential"/>
        /// to instantiate a <see cref="ClientCredential"/> used in the constructors of <see cref="ConfidentialClientApplication"/>
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        public ClientAssertionCertificateWrapper(X509Certificate2 certificate) : this(certificate, null)
        { }

        /// <summary>
        /// Gets minimum X509 certificate key size in bits
        /// </summary>
        public static int MinKeySizeInBits => 2048;

        /// <summary>
        /// Gets the X509 certificate used as credentials to prove the identity of the application to Azure AD.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        public Dictionary<string, string> ClaimsToSign { get; }

        internal byte[] Sign(ICryptographyManager cryptographyManager, string message)
        {
            return cryptographyManager.SignWithCertificate(message, Certificate);
        }

        // Thumbprint should be url encoded
        internal string Thumbprint => Base64UrlHelpers.Encode(Certificate.GetCertHash());
    }
#endif
}
