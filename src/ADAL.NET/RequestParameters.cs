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
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class RequestParameters
    {
        private Dictionary<string, SecureString> secureParameters;

        public void AddSecureParameter(string key, SecureString value)
        {
            if (this.secureParameters == null)
            {
                this.secureParameters = new Dictionary<string, SecureString>();
            }

            this.secureParameters.Add(key, value);
        }

        private void AddSecureParametersToMessageBuilder(StringBuilder messageBuilder)
        {
            if (this.secureParameters != null)
            {
                foreach (KeyValuePair<string, SecureString> kvp in this.secureParameters)
                {
                    char[] secureParameterChars = null;
                    try
                    {
                        secureParameterChars = kvp.Value.ToCharArray();
                        EncodingHelper.AddStringWithUrlEncoding(messageBuilder, kvp.Key, secureParameterChars);
                    }
                    finally
                    {
                        secureParameterChars.SecureClear();
                    }
                }
            }
        }

        private void AddClientKey(ClientKey clientKey)
        {
            if (clientKey.ClientId != null)
            {
                this[OAuthParameter.ClientId] = clientKey.ClientId;
            }

            if (clientKey.Credential != null)
            {
                if (clientKey.Credential.ClientSecret != null)
                {
                    this[OAuthParameter.ClientSecret] = clientKey.Credential.ClientSecret;
                }
                else
                {
                    this.AddSecureParameter(OAuthParameter.ClientSecret, clientKey.Credential.SecureClientSecret);
                }
            }
            else if (clientKey.Assertion != null)
            {
                this[OAuthParameter.ClientAssertionType] = clientKey.Assertion.AssertionType;
                this[OAuthParameter.ClientAssertion] = clientKey.Assertion.Assertion;
            }
            else if (clientKey.Certificate != null)
            {
                JsonWebToken jwtToken = new JsonWebToken(clientKey.Certificate, clientKey.Authenticator.SelfSignedJwtAudience);
                ClientAssertion clientAssertion = jwtToken.Sign(clientKey.Certificate);
                this[OAuthParameter.ClientAssertionType] = clientAssertion.AssertionType;
                this[OAuthParameter.ClientAssertion] = clientAssertion.Assertion;
            }
        }
    }
}
