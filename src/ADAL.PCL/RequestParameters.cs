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

using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class RequestParameters : Dictionary<string, string>
    {
        private readonly StringBuilder stringBuilderParameter;

        public RequestParameters(string resource, ClientKey clientKey)
        {
            if (!string.IsNullOrWhiteSpace(resource))
            {
                this[OAuthParameter.Resource] = resource;
            }

            this.AddClientKey(clientKey);    
        }

        public RequestParameters(StringBuilder stringBuilderParameter)
        {
            this.stringBuilderParameter = stringBuilderParameter;
            this.HasStringParameter = true;
        }

        public bool HasStringParameter { get; private set; }

        public string ExtraQueryParameter { get; set; }

        public override string ToString()
        {
            return this.ToStringBuilder().ToString();
        }

        public void WriteToStream(Stream stream)
        {
            StringBuilder stringBuilder = this.ToStringBuilder();
            byte[] data = null;

            try
            {
                data = stringBuilder.ToByteArray();
                stream.Write(data, 0, data.Length);
            }
            finally
            {
                data.SecureClear();
                stringBuilder.SecureClear();
            }
        }

        private StringBuilder ToStringBuilder()
        {
            StringBuilder messageBuilder = new StringBuilder();
            if (this.stringBuilderParameter != null)
            {
                messageBuilder.Append(this.stringBuilderParameter);
            }
            
            foreach (KeyValuePair<string, string> kvp in this)
            {
                EncodingHelper.AddKeyValueString(messageBuilder, EncodingHelper.UrlEncode(kvp.Key), EncodingHelper.UrlEncode(kvp.Value));
            }

            if (this.ExtraQueryParameter != null)
            {
                messageBuilder.Append('&' + this.ExtraQueryParameter);
            }

            return messageBuilder;
        }

        private void AddClientKey(ClientKey clientKey)
        {
            if (clientKey.ClientId != null)
            {
                this[OAuthParameter.ClientId] = clientKey.ClientId;
            }

            if (clientKey.Credential != null)
            {
                this[OAuthParameter.ClientSecret] = clientKey.Credential.ClientSecret;
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