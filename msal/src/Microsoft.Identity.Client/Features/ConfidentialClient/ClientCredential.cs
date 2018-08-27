//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Meant to be used in confidential client applications, an instance of <c>ClientCredential</c> is passed 
    /// to the constructors of (<see cref="ConfidentialClientApplication"/>)
    /// as credentials proving that the application (the client) is what it claims it is. These credentials can be
    /// either a client secret (an application password) or a certificate. 
    /// This class has one constructor for each case.
    /// These crendentials are added in the application registration portal (in the secret section).
    /// </summary>
    public sealed class ClientCredential
    {
        /// <summary>
        /// Constructor of client (application) credentials from a <see cref="ClientAssertionCertificate"/>
        /// </summary>
        /// <param name="certificate">contains information about the certificate previously shared with AAD at application
        /// registration to prove the identity of the application (the client) requesting the tokens.</param>
        public ClientCredential(ClientAssertionCertificate certificate)
        {
            Certificate = certificate;
        }

        internal ClientAssertionCertificate Certificate { get; private set; }
        internal string Assertion { get; set; }
        internal long ValidTo { get; set; }
        internal bool ContainsX5C { get; set; }
        internal string Audience { get; set; }

        /// <summary>
        /// Constructor of client (application) credentials from a client secret, also known as the application password.
        /// </summary>
        /// <param name="secret">Secret string previously shared with AAD at application registration to prove the identity
        /// of the application (the client) requesting the tokens.</param>
        public ClientCredential(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentNullException(nameof(secret));
            }

            Secret = secret;
        }

        internal string Secret { get; private set; }
    }
}