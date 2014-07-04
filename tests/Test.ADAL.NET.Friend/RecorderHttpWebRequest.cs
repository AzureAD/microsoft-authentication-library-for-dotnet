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
using System.Net;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Friend
{
    internal class RecorderHttpWebRequest : RecorderBase, IHttpWebRequest
    {
        private readonly IHttpWebRequest internalHttpWebRequest;
        private readonly Dictionary<string, string> keyElements;

        static RecorderHttpWebRequest()
        {
            Initialize();
        }

        public RecorderHttpWebRequest(string uri)
        {
            this.internalHttpWebRequest = (new HttpWebRequestFactory()).Create(uri);
            this.keyElements = new Dictionary<string, string>();
            this.keyElements["Uri"] = uri;
        }

        public RequestParameters BodyParameters
        {
            set
            {
                this.internalHttpWebRequest.BodyParameters = value;
            }

            get
            {
                return this.internalHttpWebRequest.BodyParameters;
            }
        }

        public string Accept
        {
            set
            {
                this.keyElements["Accept"] = value;
                this.internalHttpWebRequest.Accept = value;
            }
        }

        public string ContentType
        {
            set
            {
                this.keyElements["ContentType"] = value;
                this.internalHttpWebRequest.ContentType = value;
            }
        }

        public string Method
        {
            set
            {
                this.keyElements["Method"] = value;
                this.internalHttpWebRequest.Method = value;
            }
        }

        public bool UseDefaultCredentials
        {
            set
            {
                this.keyElements["UseDefaultCredentials"] = value.ToString();
                this.internalHttpWebRequest.UseDefaultCredentials = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.internalHttpWebRequest.Headers;
            }
        }

        public async Task<IHttpWebResponse> GetResponseSyncOrAsync(CallState callState)
        {
            foreach (var headerKey in this.internalHttpWebRequest.Headers.AllKeys)
            {
                this.keyElements["Header-" + headerKey] = this.internalHttpWebRequest.Headers[headerKey];
            }

            if (this.internalHttpWebRequest.BodyParameters != null)
            {
                foreach (var kvp in this.internalHttpWebRequest.BodyParameters)
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

            Stream responseStream;
            if (IOMap.ContainsKey(key))
            {
                string value = IOMap[key];
                if (value[0] == 'P')
                {
                    value = value.Substring(1);
                    return new RecorderHttpWebResponse(value, HttpStatusCode.OK);
                }
                else
                {
                    value = value.Substring(1);
                    WebException ex = SerializationHelper.DeserializeWebException(value);
                    throw ex;
                }
            }

            try
            {
                IHttpWebResponse response = await this.internalHttpWebRequest.GetResponseSyncOrAsync(callState);
                responseStream = response.GetResponseStream();
                string str = SerializationHelper.StreamToString(responseStream);
                IOMap.Add(key, 'P' + str);
                return new RecorderHttpWebResponse(str, HttpStatusCode.OK);
            }
            catch (WebException ex)
            {
                IOMap[key] = 'N' + SerializationHelper.SerializeWebException(ex);
                throw ex;
            }
        }
    }
}
