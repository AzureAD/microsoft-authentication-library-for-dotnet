// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Container returned from <c>WithClientAssertion</c>.
    /// </summary>
    public class AssertionResponse
    {
        /// <summary>Base-64 JWT that MSAL sends as <c>client_assertion</c>.</summary>
        public string Assertion { get; init; }

        /// <summary>
        /// Certificate for mutual-TLS PoP.  
        /// Leave <c>null</c> for a bearer assertion.
        /// </summary>
        public X509Certificate2 TokenBindingCertificate { get; init; }
    }
}
