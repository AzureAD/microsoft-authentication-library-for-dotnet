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
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client
{
#pragma warning disable CS1587 // XML comment is not placed on a valid language element
    /// <summary>
    /// Type containing an assertion representing a user's credentials. This type is used in the
    /// On-Behalf-Of flow in confidential client applications, enabling a Web API to request a token
    /// for another downsteam API in the name of the user whose credentials are held by this <c>UserAssertion</c>
    /// See https://aka.ms/msal-net-on-behalf-of 
    /// </summary>
#if DESKTOP || NET_CORE || NETSTANDARD1_3
    /// <seealso cref="ConfidentialClientApplication.AcquireTokenOnBehalfOfAsync(System.Collections.Generic.IEnumerable{string}, UserAssertion)"/>
    /// and <see cref="ConfidentialClientApplication.AcquireTokenOnBehalfOfAsync(System.Collections.Generic.IEnumerable{string}, UserAssertion, string)"/>
#endif
    public sealed class UserAssertion
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
    {
        /// <summary>
        /// Constructor from a JWT assertion. For other assertion types (SAML), use the other constructor <see cref="UserAssertion.UserAssertion(string, string)"/>
        /// </summary>
        /// <param name="jwtBearerToken">JWT bearer token used to access the Web application itself</param>
        public UserAssertion(string jwtBearerToken) : this(jwtBearerToken, OAuth2GrantType.JwtBearer)
        {
        }

        /// <summary>
        /// Constructor of a UserAssertion specifying the assertionType in addition to the assertion
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        /// <param name="assertionType">Type of the assertion representing the user. Accepted types are currently:
        /// <list type="bullet">
        /// <item>urn:ietf:params:oauth:grant-type:jwt-bearer<term></term><description>JWT bearer token. Passing this is equivalent to using 
        /// the other (simpler) constructor</description></item>
        /// <item>urn:ietf:params:oauth:grant-type:saml1_1-bearer<term></term><description>SAML 1.1 bearer token</description></item>
        /// <item>urn:ietf:params:oauth:grant-type:jwt-bearer<term></term><description>SAML 2 bearer token</description></item>
        /// </list></param>
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

            var crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;

            AssertionType = assertionType;
            Assertion = assertion;
            AssertionHash =
                crypto.CreateBase64UrlEncodedSha256Hash(Assertion);
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