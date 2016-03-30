//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
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

        public IRequestParameters BodyParameters
        {
            set { this.internalHttpCilent.BodyParameters = value; }

            get { return this.internalHttpCilent.BodyParameters; }
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
            set { this.keyElements["UseDefaultCredentials"] = value.ToString(); }
        }

        public Dictionary<string, string> Headers
        {
            get { return this.internalHttpCilent.Headers; }
        }

        public CallState CallState { get; set; }

        public async Task<IHttpWebResponse> GetResponseAsync()
        {
            return await Task.Factory.StartNew(() =>
            {
                foreach (var headerKey in this.internalHttpCilent.Headers.Keys)
                {
                    this.keyElements["Header-" + headerKey] = this.internalHttpCilent.Headers[headerKey];
                }

                if (this.CallState != null)
                {
                    this.keyElements["Header-CorrelationId"] = this.CallState.CorrelationId.ToString();
                }

                if (this.internalHttpCilent.BodyParameters is DictionaryRequestParameters)
                {
                    foreach (var kvp in (DictionaryRequestParameters) this.internalHttpCilent.BodyParameters)
                    {
                        string value = (kvp.Key == "password") ? "PASSWORD" : kvp.Value;
                        this.keyElements["Body-" + kvp.Key] = value;
                    }
                }

                string key = string.Empty;
                foreach (var kvp in this.keyElements)
                {
                    key += string.Format(CultureInfo.CurrentCulture, " {0}={1},", kvp.Key, kvp.Value);
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
            });
        }
    }
}
