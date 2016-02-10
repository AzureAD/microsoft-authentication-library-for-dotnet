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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class HttpClientWrapper
    {
        private readonly string uri;
        private int timeoutInMilliSeconds = 30000;

        private static readonly Lazy<HttpClient> clientForUsingCredential =
            new Lazy<HttpClient>(() => new HttpClient(new HttpClientHandler {UseDefaultCredentials = true}));

        private static readonly Lazy<HttpClient> clientWithoutCredential =
            new Lazy<HttpClient>(() => new HttpClient(new HttpClientHandler {UseDefaultCredentials = false}));

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
            set { this.timeoutInMilliSeconds = value; }
        }


        public async Task<IHttpWebResponse> GetResponseAsync()
        {
            if (UseDefaultCredentials)
            {
                return await GetResponseAsync(clientForUsingCredential.Value).ConfigureAwait(false);
            }
            else
            {
                return await GetResponseAsync(clientWithoutCredential.Value).ConfigureAwait(false);
            }
        }

        public async Task<IHttpWebResponse> GetResponseAsync(HttpClient client)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            requestMessage.RequestUri = new Uri(uri);
            requestMessage.Headers.Accept.Clear();

            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(this.Accept ?? "application/json"));
            foreach (KeyValuePair<string, string> kvp in this.Headers)
            {
                requestMessage.Headers.Add(kvp.Key, kvp.Value);
            }

            bool addCorrelationId = (this.CallState != null && this.CallState.CorrelationId != Guid.Empty);
            if (addCorrelationId)
            {
                requestMessage.Headers.Add(OAuthHeader.CorrelationId, this.CallState.CorrelationId.ToString());
                requestMessage.Headers.Add(OAuthHeader.RequestCorrelationIdInResponse, "true");
            }
            
            if(client.Timeout != TimeSpan.FromMilliseconds(this.timeoutInMilliSeconds))
            {
                client.Timeout = TimeSpan.FromMilliseconds(this.timeoutInMilliSeconds);
            }

            HttpResponseMessage responseMessage;

            try
            {
                if (this.BodyParameters != null)
                {
                    HttpContent content;
                    if (this.BodyParameters is StringRequestParameters)
                    {
                        content = new StringContent(this.BodyParameters.ToString(), Encoding.UTF8, this.ContentType);
                    }
                    else
                    {
                        content = new FormUrlEncodedContent(((DictionaryRequestParameters) this.BodyParameters).ToList());
                    }

                    requestMessage.Method = HttpMethod.Post;
                    requestMessage.Content = content;
                }
                else
                {
                    requestMessage.Method = HttpMethod.Get;
                }

                responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                throw new MsalException(MsalError.HttpRequestCancelled, ex);
            }

            IHttpWebResponse webResponse = await CreateResponseAsync(responseMessage).ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                try
                {
                    throw new HttpRequestException(
                        string.Format("Response status code does not indicate success: {0} ({1}).",
                            (int) webResponse.StatusCode, webResponse.StatusCode));
                }
                catch (HttpRequestException ex)
                {
                    webResponse.ResponseStream.Position = 0;
                    //TODO remove stream.position and fix MSALServiceException
                    //throw new MsalServiceException(webResponse, ex);
                }
            }

            if (addCorrelationId)
            {
                VerifyCorrelationIdHeaderInReponse(webResponse.Headers);
            }

            return webResponse;
        }

        public static async Task<IHttpWebResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>();
            if (response.Headers != null)
            {
                foreach (var kvp in response.Headers)
                {
                    headers[kvp.Key] = kvp.Value.First();
                }
            }

            return new MsalHttpWebResponse(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), headers,
                response.StatusCode);
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
                        PlatformPlugin.Logger.Warning(CallState,
                            string.Format("Returned correlation id '{0}' is not in GUID format.", correlationIdHeader));
                    }
                    else if (correlationIdInResponse != this.CallState.CorrelationId)
                    {
                        PlatformPlugin.Logger.Warning(
                            this.CallState,
                            string.Format("Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader, CallState.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}