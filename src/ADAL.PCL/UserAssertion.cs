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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Credential type containing an assertion representing user credential.
    /// </summary>
    public sealed class UserAssertion
    {
        /// <summary>
        /// Constructor to create the object with an assertion. This constructor can be used for On Behalf Of flow which assumes the
        /// assertion is a JWT token. For other flows, the other construction with assertionType must be used.
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        public UserAssertion(string assertion) :this(assertion, OAuthGrantType.JwtBearer)
        {
        }

        /// <summary>
        /// Constructor to create credential with client id, assertion and assertionType
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        /// <param name="assertionType">Type of the assertion representing the user.</param>
        public UserAssertion(string assertion, string assertionType)
            : this(assertion, assertionType, null)
        {
        }

        /// <summary>
        /// Constructor to create credential with client id, assertion, assertionType and userId
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        /// <param name="assertionType">Type of the assertion representing the user.</param>
        /// <param name="userName">Identity of the user token is requested for. This parameter can be null.</param>
        public UserAssertion(string assertion, string assertionType, string userName)
        {
            if (string.IsNullOrWhiteSpace(assertion))
            {
                throw new ArgumentNullException("assertion");
            }

            if (string.IsNullOrWhiteSpace(assertionType))
            {
                throw new ArgumentNullException("assertionType");
            }

            this.AssertionType = assertionType;
            this.Assertion = assertion;
            this.UserName = userName;
        }

        /// <summary>
        /// Gets the assertion.
        /// </summary>
        public string Assertion { get; private set; }

        /// <summary>
        /// Gets the assertion type.
        /// </summary>
        public string AssertionType { get; private set; }

        /// <summary>
        /// Gets name of the user.
        /// </summary>
        public string UserName { get; internal set; }
    }
}
