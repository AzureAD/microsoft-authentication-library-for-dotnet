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
    /// Credential type containing an assertion representing user credential.
    /// </summary>
    public sealed class UserAssertion
    {
        /// <summary>
        /// Constructor to create the object with an assertion. This constructor can be used for On Behalf Of flow which assumes the
        /// assertion is a JWT token. For other flows, the other construction with assertionType must be used.
        /// </summary>
        /// <param name="assertion">Assertion representing the user.</param>
        public UserAssertion(string assertion)
        {
            if (string.IsNullOrWhiteSpace(assertion))
            {
                throw new ArgumentNullException("assertion");
            }

            this.Assertion = assertion;
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
