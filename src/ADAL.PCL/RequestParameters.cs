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
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal interface IRequestParameters
    {
    }

    internal class DictionaryRequestParameters : Dictionary<string, string>, IRequestParameters
    {
        public DictionaryRequestParameters(string resource, ClientKey clientKey)
        {
            if (!string.IsNullOrWhiteSpace(resource))
            {
                this[OAuthParameter.Resource] = resource;
            }

            clientKey.AddToParameters(this);    
        }

        public string ExtraQueryParameter { get; set; }

        public override string ToString()
        {
            StringBuilder messageBuilder = new StringBuilder();
            
            foreach (KeyValuePair<string, string> kvp in this)
            {
                EncodingHelper.AddKeyValueString(messageBuilder, EncodingHelper.UrlEncode(kvp.Key), EncodingHelper.UrlEncode(kvp.Value));
            }

            if (this.ExtraQueryParameter != null)
            {
                messageBuilder.Append('&' + this.ExtraQueryParameter);
            }

            return messageBuilder.ToString();
        }
    }

    internal class StringRequestParameters : IRequestParameters
    {
        private readonly StringBuilder parameter;

        public StringRequestParameters(StringBuilder stringBuilderParameter)
        {
            this.parameter = stringBuilderParameter;
        }

        public override string ToString()
        {
            return this.parameter.ToString();
        }
    }
}