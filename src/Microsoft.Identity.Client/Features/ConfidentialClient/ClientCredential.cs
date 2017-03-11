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
    /// Meant to be used in confidential client applications. Allows developers to 
    /// pass either client secret or client assertion certificate of their application.
    /// </summary>
    public sealed class ClientCredential
    {
#if !NETSTANDARD1_1
        /// <summary>
        /// Constructor provide client assertion certificate
        /// </summary>
        /// <param name="certificate">certificate of the client requesting the token.</param>
        public ClientCredential(ClientAssertionCertificate certificate)
        {
            this.Certificate = certificate;
        }

        internal ClientAssertionCertificate Certificate { get; private set; }
        internal string Assertion { get; set; }
        internal long ValidTo { get; set; }
#endif

        /// <summary>
        /// Constructor to provide client secret
        /// </summary>
        /// <param name="secret">Secret of the client requesting the token.</param>
        public ClientCredential(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentNullException("secret");
            }

            this.Secret = secret;
        }

        internal string Secret { get; private set; }
    }
}