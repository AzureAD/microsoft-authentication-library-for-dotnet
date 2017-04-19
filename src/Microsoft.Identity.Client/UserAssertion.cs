//------------------------------------------------------------------------------
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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Credential type containing an assertion representing user credential.
    /// </summary>
    public sealed class UserAssertion
    {
        /// <summary>
        /// Constructor to create the object with an assertion. This constructor can be used for On Behalf Of flow which
        /// assumes the
        /// assertion is a JWT token. For other flows, the other construction with assertionType must be used.
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        public UserAssertion(string assertion) : this(assertion, OAuth2GrantType.JwtBearer)
        {
        }

        /// <summary>
        /// Constructor to create credential with assertion and assertionType
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        /// <param name="assertionType">Type of the assertion representing the user.</param>
        public UserAssertion(string assertion, string assertionType)
        {
            if (string.IsNullOrWhiteSpace(assertion))
            {
                throw new ArgumentNullException(nameof(assertion));
            }

            if (string.IsNullOrWhiteSpace(assertionType))
            {
                throw new ArgumentNullException(nameof(assertionType));
            }

            AssertionType = assertionType;
            Assertion = assertion;
            AssertionHash =
                CryptographyHelper.CreateBase64UrlEncodedSha256Hash(Assertion);
        }

        /// <summary>
        /// Gets the assertion.
        /// </summary>
        public string Assertion { get; private set; }

        /// <summary>
        /// Gets the assertion type.
        /// </summary>
        public string AssertionType { get; private set; }

        internal string AssertionHash { get; set; }
    }
}