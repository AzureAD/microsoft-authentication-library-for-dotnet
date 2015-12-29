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
using System.Collections.Generic;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public enum ClientCredentialType
    {
        ClientSecret, //client_secret
        ClientAssertion, //urn:ietf:params:oauth:client-assertion-type:jwt-bearer
    }

    /// <summary>
    /// Credential including client id and secret.
    /// </summary>
    public sealed class ClientCredential
    {
        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="credential">Secret of the client requesting the token.</param>
        public ClientCredential(string credential, ClientCredentialType clientCredentialType)
        {

            if (string.IsNullOrWhiteSpace(credential))
            {
                throw new ArgumentNullException("credential");
            }
            
            this.Credential = credential;
            this.ClientCredentialType = clientCredentialType;
        }

        internal string ClientId { get; set; }

        internal string Credential { get; private set; }

        internal ClientCredentialType ClientCredentialType { get; private set; }


        internal void AddToParameters(IDictionary<string, string> parameters)
        {
            if (this.ClientId != null)
            {
                parameters[OAuthParameter.ClientId] = this.ClientId;
            }

            if (this.ClientCredentialType == ClientCredentialType.ClientSecret)
            {
                parameters[OAuthParameter.ClientSecret] = this.Credential;
            }
            else if (this.ClientCredentialType == ClientCredentialType.ClientAssertion)
            {
                //TODO - handle JWT certificate assertion
                /*JsonWebToken jwtToken = new JsonWebToken(this.Certificate, this.Authenticator.SelfSignedJwtAudience);
                ClientAssertion clientAssertion = jwtToken.Sign(this.Certificate);*/
                parameters[OAuthParameter.ClientAssertionType] = OAuthAssertionType.JwtBearer;
                parameters[OAuthParameter.ClientAssertion] = this.Credential;
            }
        }
    }
}
