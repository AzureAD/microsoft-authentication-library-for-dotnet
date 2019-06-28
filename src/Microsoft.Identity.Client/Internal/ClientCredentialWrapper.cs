// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Meant to be used in confidential client applications, an instance of <c>ClientCredential</c> is passed
    /// to the constructors of (<see cref="ConfidentialClientApplication"/>)
    /// as credentials proving that the application (the client) is what it claims it is. These credentials can be
    /// either a client secret (an application password) or a certificate.
    /// This class has one constructor for each case.
    /// These credentials are added in the application registration portal (in the secret section).
    /// </summary>
    internal sealed class ClientCredentialWrapper
    {
        public ClientCredentialWrapper(ApplicationConfiguration config)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            ValidateCredentialParameters(config);

            switch (AuthenticationType)
            {
            case ConfidentialClientAuthenticationType.ClientCertificate:
                Certificate = config.ClientCredentialCertificate;
                break;
            case ConfidentialClientAuthenticationType.ClientCertificateWithClaims:
                Certificate = config.ClientCredentialCertificate;
                ClaimsToSign = config.ClaimsToSign;
                break;
            case ConfidentialClientAuthenticationType.ClientSecret:
                Secret = config.ClientSecret;
                break;
            case ConfidentialClientAuthenticationType.SignedClientAssertion:
                SignedAssertion = config.SignedClientAssertion;
                break;
            }
        }

        #region TestBuilders
        //The following builders methods are inteded for testing
        public static ClientCredentialWrapper CreateWithCertificate(X509Certificate2 certificate, IDictionary<string, string> claimsToSign = null)
        {
            return new ClientCredentialWrapper(certificate, claimsToSign);
        }

        public static ClientCredentialWrapper CreateWithSecret(string secret)
        {
            var app = new ClientCredentialWrapper(secret, ConfidentialClientAuthenticationType.ClientSecret);
            app.AuthenticationType = ConfidentialClientAuthenticationType.ClientSecret;
            return app;
        }

        public static ClientCredentialWrapper CreateWithSignedClientAssertion(string signedClientAssertion)
        {
            var app = new ClientCredentialWrapper(signedClientAssertion, ConfidentialClientAuthenticationType.SignedClientAssertion);
            app.AuthenticationType = ConfidentialClientAuthenticationType.SignedClientAssertion;
            return app;
        }

        private ClientCredentialWrapper(X509Certificate2 certificate, IDictionary<string, string> claimsToSign = null)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            Certificate = certificate;

            if (claimsToSign != null && claimsToSign.Any())
            {
                ClaimsToSign = claimsToSign;
                AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificateWithClaims;
                return;
            }

            AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificate;
        }

        private ClientCredentialWrapper(string secretOrAssertion, ConfidentialClientAuthenticationType authType)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            if (authType == ConfidentialClientAuthenticationType.SignedClientAssertion)
            {
                SignedAssertion = secretOrAssertion;
            }
            else
            {
                Secret = secretOrAssertion;
            }
        }

        #endregion TestBuilders
        private void CheckCertificateKeySize(X509Certificate2 cert)
        {
            //Currently, the min key size is enforced on desktop (net 45) as 2048 as it is the current industry standard.
            //This is not enforced on netCore unfortunalty and adding this enforcement may break customers on netCore.
            //NetCore can only enforce a min key size of 512 since it is enforced by the portal already.
#if DESKTOP
            if (cert.PublicKey.Key.KeySize < MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException(nameof(cert),
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.CertificateKeySizeTooSmallTemplate,
                        MinKeySizeInBits));
            }
#endif
        }

        private void ValidateCredentialParameters(ApplicationConfiguration config)
        {
            if (config.ConfidentialClientCredentialCount > 1)
            {
                throw new MsalClientException(MsalError.ClientCredentialAuthenticationTypesAreMutuallyExclusive, MsalErrorMessage.ClientCredentialAuthenticationTypesAreMutuallyExclusive);
            }

            if (!string.IsNullOrWhiteSpace(config.ClientSecret))
            {
                AuthenticationType = ConfidentialClientAuthenticationType.ClientSecret;
            }

            if (config.ClientCredentialCertificate != null)
            {
                if (config.ClaimsToSign != null && config.ClaimsToSign.Any())
                {
                    AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificateWithClaims;
                    AppendDefaultClaims = config.MergeWithDefaultClaims;
                }
                else
                {
                    AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificate;
                }
            }

            if (!string.IsNullOrWhiteSpace(config.SignedClientAssertion))
            {
                AuthenticationType = ConfidentialClientAuthenticationType.SignedClientAssertion;
            }
        }

        internal byte[] Sign(ICryptographyManager cryptographyManager, string message)
        {
            return cryptographyManager.SignWithCertificate(message, Certificate);
        }

        private static readonly int s_minKeySizeInBits = 2048;
        public static int MinKeySizeInBits { get { return s_minKeySizeInBits; } }
        internal string Thumbprint { get { return Base64UrlHelpers.Encode(Certificate.GetCertHash()); } }
        internal X509Certificate2 Certificate { get; private set; }
        // The cached assertion created from the JWT signing operation
        internal string CachedAssertion { get; set; }
        internal long ValidTo { get; set; }
        internal bool ContainsX5C { get; set; }
        internal string Audience { get; set; }
        internal string Secret { get; private set; }
        // The signed assertion passed in by the user
        internal string SignedAssertion { get; private set; }
        internal bool AppendDefaultClaims { get; private set; }
        internal ConfidentialClientAuthenticationType AuthenticationType { get; private set; }
        internal IDictionary<string, string> ClaimsToSign { get; private set; }
    }

    internal enum ConfidentialClientAuthenticationType
    {
        ClientCertificate,
        ClientCertificateWithClaims,
        ClientSecret,
        SignedClientAssertion
    }
#endif
}
