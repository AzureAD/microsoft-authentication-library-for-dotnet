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
using System.Net;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.Common;

namespace Test.ADAL.WinRT.Unit
{
    internal class ReplayerHttpClient : ReplayerBase, IHttpClient
    {
        private readonly IHttpClient internalHttpCilent;
        private readonly Dictionary<string, string> keyElements;

        public ReplayerHttpClient(string uri, CallState callState)
        {
            this.internalHttpCilent = (new HttpClientFactory()).Create(uri, null);
            this.keyElements = new Dictionary<string, string>();
            this.keyElements["Uri"] = uri;
            this.CallState = callState;
        }

        public RequestParameters BodyParameters
        {
            set
            {
                this.internalHttpCilent.BodyParameters = value;
            }

            get
            {
                return this.internalHttpCilent.BodyParameters;
            }
        }

        public string Accept
        {
            set
            {
                this.keyElements["Accept"] = value;
                this.internalHttpCilent.Accept = value;
            }
        }

        public string ContentType
        {
            set
            {
                this.keyElements["ContentType"] = value;
                this.internalHttpCilent.ContentType = value;
            }
        }

        public bool UseDefaultCredentials
        {
            set
            {
                this.keyElements["UseDefaultCredentials"] = value.ToString();
            }
        }

        public Dictionary<string, string> Headers
        {
            get
            {
                return this.internalHttpCilent.Headers;
            }
        }

        public CallState CallState { get; set; }

        public async Task<IHttpWebResponse> GetResponseAsync()
        {
            foreach (var headerKey in this.internalHttpCilent.Headers.Keys)
            {
                this.keyElements["Header-" + headerKey] = this.internalHttpCilent.Headers[headerKey];
            }

            if (this.CallState != null)
            {
                this.keyElements["Header-CorrelationId"] = this.CallState.CorrelationId.ToString();
            }

            if (this.internalHttpCilent.BodyParameters != null)
            {
                foreach (var kvp in this.internalHttpCilent.BodyParameters)
                {
                    string value = (kvp.Key == "password") ? "PASSWORD" : kvp.Value;
                    this.keyElements["Body-" + kvp.Key] = value;
                }
            }

            string key = string.Empty;
            foreach (var kvp in this.keyElements)
            {
                key += string.Format("{0}={1},", kvp.Key, kvp.Value);
            }

            if (IOMap.ContainsKey(key))
            {
                string value = IOMap[key];
                if (value[0] == 'P')
                {
                    value = value.Substring(1);
                    return new ReplayerHttpWebResponse(value, HttpStatusCode.OK);
                }

                throw SerializationHelper.DeserializeException(value.Substring(1));
            }

            throw new Exception("There is no recorded response to replay");
        }
    }
}
