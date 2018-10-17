// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http
{
    internal class HttpClientWrapper : IHttpClient
    {
        private const string FormUrlEncoded = "application/x-www-form-urlencoded";
        private readonly long _maxResponseSizeInBytes = 1048576;
        private readonly string uri;

        public HttpClientWrapper(string uri, RequestContext requestContext)
        {
            this.uri = uri;
            Headers = new Dictionary<string, string>();
            RequestContext = requestContext;
        }

        protected RequestContext RequestContext { get; set; }
        public int TimeoutInMilliSeconds { set; get; } = 30000;
        public IRequestParameters BodyParameters { get; set; }
        public string Accept { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; private set; }

        public async Task<IHttpWebResponse> GetResponseAsync()
        {
            // TODO: UseDefaultCredentials is TRUE in MSAL but has always been FALSE in adal.  need to reconcile.
            using (var client = new HttpClient(AdalHttpMessageHandlerFactory.GetMessageHandler(false)))
            {
                client.MaxResponseContentBufferSize = _maxResponseSizeInBytes;
                client.DefaultRequestHeaders.Accept.Clear();
                var requestMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri(uri)
                };
                requestMessage.Headers.Accept.Clear();

                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Accept ?? "application/json"));
                foreach (KeyValuePair<string, string> kvp in Headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }

                bool addCorrelationId = RequestContext != null && RequestContext.Logger.CorrelationId != Guid.Empty;
                if (addCorrelationId)
                {
                    requestMessage.Headers.Add(OAuthHeader.CorrelationId, RequestContext.Logger.CorrelationId.ToString());
                    requestMessage.Headers.Add(OAuthHeader.RequestCorrelationIdInResponse, "true");
                }

                client.Timeout = TimeSpan.FromMilliseconds(TimeoutInMilliSeconds);

                HttpResponseMessage responseMessage;

                try
                {
                    if (BodyParameters != null)
                    {
                        HttpContent content;
                        if (BodyParameters is StringRequestParameters)
                        {
                            content = new StringContent(BodyParameters.ToString(), Encoding.UTF8, ContentType);
                        }
                        else
                        {
                            content = new StringContent(BodyParameters.ToString(), Encoding.UTF8, FormUrlEncoded);
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
                    throw new HttpRequestWrapperException(null, ex);
                }

                var webResponse = await CreateResponseAsync(responseMessage).ConfigureAwait(false);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new HttpRequestWrapperException(
                        webResponse,
                        new HttpRequestException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "Response status code does not indicate success: {0} ({1}).",
                                (int)webResponse.StatusCode,
                                webResponse.StatusCode),
                            new AdalException(webResponse.Body)));
                }

                if (addCorrelationId)
                {
                    VerifyCorrelationIdHeaderInReponse(webResponse.Headers);
                }

                return webResponse;
            }
        }

        public static async Task<IHttpWebResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            return new HttpWebResponseWrapper(
                await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                response.Headers,
                response.StatusCode);
        }

        private void VerifyCorrelationIdHeaderInReponse(HttpResponseHeaders headers)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                string responseHeaderKey = header.Key;
                string trimmedKey = responseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuthHeader.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers.GetValues(trimmedKey).FirstOrDefault().Trim();
                    if (!Guid.TryParse(correlationIdHeader, out var correlationIdInResponse))
                    {
                        RequestContext.Logger.Warning(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "Returned correlation id '{0}' is not in GUID format.",
                                correlationIdHeader));
                    }
                    else if (correlationIdInResponse != RequestContext.Logger.CorrelationId)
                    {
                        RequestContext.Logger.Warning(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader,
                                RequestContext.Logger.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}