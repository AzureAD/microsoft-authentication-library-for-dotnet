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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class HttpClientWrapper : IHttpClient
    {
        private readonly string uri;
        private int timeoutInMilliSeconds = 30000;

        public HttpClientWrapper(string uri, CallState callState)
        {
            this.uri = uri;
            this.Headers = new Dictionary<string, string>();
            this.CallState = callState;
        }

        protected CallState CallState { get; set; }

        public IRequestParameters BodyParameters { get; set; }

        public string Accept { get; set; }

        public string ContentType { get; set; }

        public bool UseDefaultCredentials { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

        public int TimeoutInMilliSeconds
        {
            set
            {
                this.timeoutInMilliSeconds = value;
            }
        }

        public async Task<IHttpWebResponse> GetResponseAsync()
        {
            using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = this.UseDefaultCredentials }))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(this.Accept ?? "application/json"));
                foreach (KeyValuePair<string, string> kvp in this.Headers)
                {
                    client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
                }

                bool addCorrelationId = (this.CallState != null && this.CallState.CorrelationId != Guid.Empty);
                if (addCorrelationId)
                {
                    client.DefaultRequestHeaders.Add(OAuthHeader.CorrelationId, this.CallState.CorrelationId.ToString());
                    client.DefaultRequestHeaders.Add(OAuthHeader.RequestCorrelationIdInResponse, "true");                   
                }

                client.Timeout = TimeSpan.FromMilliseconds(this.timeoutInMilliSeconds);

                HttpResponseMessage responseMessage;
                HttpContent content=null;

                bool getRequest = false;
                try
                {
                    if (this.BodyParameters != null)
                    {
                        
                        if (this.BodyParameters is StringRequestParameters)
                        {

                            content = new StringContent(this.BodyParameters.ToString(), Encoding.UTF8, this.ContentType);
                        }
                        else
                        {
                            content = new FormUrlEncodedContent(((DictionaryRequestParameters)this.BodyParameters).ToList());
                        }

                        responseMessage = await client.PostAsync(uri, content);
                    }
                    else
                    {
                        responseMessage = await client.GetAsync(uri);
                        getRequest = true;
                    }
                }
                catch (TaskCanceledException ex)
                {
                    throw new HttpRequestWrapperException(null, ex);
                }

                IHttpWebResponse webResponse = await CreateResponseAsync(responseMessage);

                // Retry the request here
                if ((int) webResponse.StatusCode == 500 || (int) webResponse.StatusCode == 503 ||
                    (int) webResponse.StatusCode == 504)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    if (getRequest)
                    {
                        responseMessage = await client.GetAsync(uri);
                    }
                    else
                    {
                        responseMessage = await client.PostAsync(uri, content);
                    }
                }

                webResponse = await CreateResponseAsync(responseMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    try
                    {
                        throw new HttpRequestException(string.Format(CultureInfo.CurrentCulture, " Response status code does not indicate success: {0} ({1}).", (int)webResponse.StatusCode, webResponse.StatusCode));
                    }
                    catch (HttpRequestException ex)
                    {
                        webResponse.ResponseStream.Position = 0;
                        throw new HttpRequestWrapperException(webResponse, ex);
                    }
                }

                if (addCorrelationId)
                {
                    VerifyCorrelationIdHeaderInReponse(webResponse.Headers);
                }

                return webResponse;
            }
        }

        public async static Task<IHttpWebResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>();
            if (response.Headers != null)
            {
                foreach (var kvp in response.Headers)
                {
                    headers[kvp.Key] = kvp.Value.First();
                }
            }

            return new HttpWebResponseWrapper(await response.Content.ReadAsStreamAsync(), headers, response.StatusCode);
        }

        private void VerifyCorrelationIdHeaderInReponse(Dictionary<string, string> headers)
        {
            foreach (string reponseHeaderKey in headers.Keys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuthHeader.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    Guid correlationIdInResponse;
                    if (!Guid.TryParse(correlationIdHeader, out correlationIdInResponse))
                    {
                        PlatformPlugin.Logger.Warning(CallState, string.Format(CultureInfo.CurrentCulture, "Returned correlation id '{0}' is not in GUID format.", correlationIdHeader));
                    }
                    else if (correlationIdInResponse != this.CallState.CorrelationId)
                    {
                        PlatformPlugin.Logger.Warning(
                            this.CallState,
                            string.Format(CultureInfo.CurrentCulture, "Returned correlation id '{0}' does not match the sent correlation id '{1}'", correlationIdHeader, CallState.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}
