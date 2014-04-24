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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Credential type containing an assertion of type "urn:ietf:params:oauth:token-type:jwt".
    /// </summary>
    public sealed class ClientAssertion
    {
        /// <summary>
        /// Constructor to create credential with a jwt token encoded as a base64 url encoded string.
        /// </summary>
        /// <param name="assertion">The jwt used as credential.</param>
        public ClientAssertion(string assertion)
            : this(assertion, OAuthAssertionType.JwtBearer)
        {
        }

        internal ClientAssertion(string assertion, string assertionType)
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
        }

        /// <summary>
        /// Gets the assertion.
        /// </summary>
        public string Assertion { get; private set; }

        /// <summary>
        /// Gets the assertion type.
        /// </summary>
        public string AssertionType { get; private set; }
    }
}
