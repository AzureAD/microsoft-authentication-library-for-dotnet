// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

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
        public static ClientCredentialWrapper CreateWithCertificate(ClientAssertionCertificateWrapper certificate)
        {
           return new ClientCredentialWrapper(certificate);
        }
        public static ClientCredentialWrapper CreateWithSecret(string secret)
        {
            return new ClientCredentialWrapper(secret, false);
        }
        public static ClientCredentialWrapper CreateWithSignedClientAssertion(string signedClientAssertion)
        {
            return new ClientCredentialWrapper(signedClientAssertion, true);
        }

        private ClientCredentialWrapper(ClientAssertionCertificateWrapper certificate)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
            Certificate = certificate;
        }

        private ClientCredentialWrapper(string secretOrAssertion, bool isSignedAssertion)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            if (string.IsNullOrWhiteSpace(secretOrAssertion))
            {
                throw new ArgumentNullException(nameof(secretOrAssertion));
            }

            if (isSignedAssertion)
            {
                SignedAssertion = secretOrAssertion;
            }
            else
            {
                Secret = secretOrAssertion;
            }
        }

        internal ClientAssertionCertificateWrapper Certificate { get; private set; }
        internal string Assertion { get; set; }
        internal long ValidTo { get; set; }
        internal bool ContainsX5C { get; set; }
        internal string Audience { get; set; }
        internal string Secret { get; private set; }
        internal string SignedAssertion { get; set; }
    }
#endif
}
