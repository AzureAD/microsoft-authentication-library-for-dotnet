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
    /// Credential including client id and secret.
    /// </summary>
    public sealed class ClientCredential
    {
        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="clientSecret">Secret of the client requesting the token.</param>
        public ClientCredential(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentNullException("clientSecret");
            }

            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
        }

        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="secureClientSecret">Secure secret of the client requesting the token.</param>
        public ClientCredential(string clientId, ISecureClientSecret secureClientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (secureClientSecret == null)
            {
                throw new ArgumentNullException("clientSecret");
            }

            this.ClientId = clientId;
            this.SecureClientSecret = secureClientSecret;
        }

        /// <summary>
        /// Gets the identifier of the client requesting the token.
        /// </summary>
        public string ClientId { get; private set; }

        internal string ClientSecret { get; private set; }

        internal ISecureClientSecret SecureClientSecret { get; private set; }
    }
}
