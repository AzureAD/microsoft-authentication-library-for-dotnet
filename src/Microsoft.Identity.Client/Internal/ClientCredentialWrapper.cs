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
        /// <summary>
        /// Constructor of client (application) credentials from a <see cref="ClientAssertionCertificateWrapper"/>
        /// </summary>
        /// <param name="certificate">contains information about the certificate previously shared with AAD at application
        /// registration to prove the identity of the application (the client) requesting the tokens.</param>
        public ClientCredentialWrapper(ClientAssertionCertificateWrapper certificate)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
            Certificate = certificate;
        }

        internal ClientAssertionCertificateWrapper Certificate { get; private set; }
        internal string Assertion { get; set; }
        internal long ValidTo { get; set; }
        internal bool ContainsX5C { get; set; }
        internal string Audience { get; set; }

        /// <summary>
        /// Constructor of client (application) credentials from a client secret, also known as the application password.
        /// </summary>
        /// <param name="secret">Secret string previously shared with AAD at application registration to prove the identity
        /// of the application (the client) requesting the tokens.</param>
        public ClientCredentialWrapper(string secret)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentNullException(nameof(secret));
            }

            Secret = secret;
        }

        public ClientCredentialWrapper(ClientAssertion clientAssertion)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            if (clientAssertion == null)
            {
                throw new ArgumentNullException(nameof(clientAssertion));
            }

            Certificate = new ClientAssertionCertificateWrapper(clientAssertion.Certificate);

            UserProvidedClientAssertion = clientAssertion;
        }

        internal string Secret { get; private set; }

        internal ClientAssertion UserProvidedClientAssertion { get; set; }
    }
#endif
}
