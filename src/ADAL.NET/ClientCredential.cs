//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Security;

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
        /// Constructor to create credential with client id and secret. This constructor accepts client secret as SecureString.
        /// </summary>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="secureClientSecret">Secret of the client requesting the token in form of SecureString.</param>
        public ClientCredential(string clientId, SecureString secureClientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            this.ClientId = clientId;
            this.SecureClientSecret = secureClientSecret;
        }

        internal SecureString SecureClientSecret { get; private set; }

        /// <summary>
        /// Gets the identifier of the client requesting the token.
        /// </summary>
        public string ClientId { get; private set; }

        internal string ClientSecret { get; private set; }
    }
}
