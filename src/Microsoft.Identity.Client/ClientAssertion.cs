// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Type containing an assertion representing a clients's credentials. This type is used to allow specific claims to be added to the authentication request.<c>UserAssertion</c>
    /// See https://aka.ms/msal-net-client-credentials
    /// </summary>
    public sealed class ClientAssertion
    {
        /// <summary>
        /// Constructor from an X509Certificate2 certificate previously registered in AAD and specific claims.
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="claims"></param>
        public ClientAssertion(X509Certificate2 certificate, Dictionary<string, string> claims)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (claims == null || claims.Count == 0)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            Certificate = certificate;
            Claims = claims;
        }

        /// <summary>
        /// Constructor from a signed client assertion.
        /// </summary>
        /// <param name="signedAssertion"></param>
        public ClientAssertion(string signedAssertion)
        {
            if (String.IsNullOrEmpty(signedAssertion))
            {
                throw new ArgumentNullException(nameof(signedAssertion));
            }

            SignedAssertion = signedAssertion;
        }

        /// <summary>
        /// Gets the claims
        /// </summary>
        public Dictionary<string, string> Claims { get; set; }

        /// <summary>
        /// Gets the certificate
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Gets the Signed Assertion
        /// </summary>
        public string SignedAssertion { get; set; }
    }
}
