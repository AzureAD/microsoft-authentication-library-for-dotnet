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
    /// 
    /// </summary>
    public sealed class ClientAssertion
    {
        /// <summary>
        /// 
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
        /// Gets the assertion.
        /// </summary>
        public string Assertion { get; private set; }

        internal string AssertionHash { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Claims { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public X509Certificate2 Certificate { get; set; }
    }
}
