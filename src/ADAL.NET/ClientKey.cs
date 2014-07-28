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
    internal partial class ClientKey
    {
        public ClientKey(ClientCredential clientCredential)
        {
            if (clientCredential == null)
            {
                throw new ArgumentNullException("clientCredential");
            }

            this.Credential = clientCredential;
            this.ClientId = clientCredential.ClientId;
            this.HasCredential = true;
        }

        public ClientKey(ClientAssertionCertificate clientCertificate, Authenticator authenticator)
        {
            this.Authenticator = authenticator;

            if (clientCertificate == null)
            {
                throw new ArgumentNullException("clientCertificate");
            }

            this.Certificate = clientCertificate;
            this.ClientId = clientCertificate.ClientId;
            this.HasCredential = true;
        }

        public ClientKey(ClientAssertion clientAssertion)
        {
            if (clientAssertion == null)
            {
                throw new ArgumentNullException("clientAssertion");
            }

            this.Assertion = clientAssertion;
            this.ClientId = clientAssertion.ClientId;
            this.HasCredential = true;
        }

        public ClientCredential Credential { get; private set; }

        public ClientAssertionCertificate Certificate { get; private set; }

        public ClientAssertion Assertion { get; private set; }

        public Authenticator Authenticator { get; private set; }
    }
}
