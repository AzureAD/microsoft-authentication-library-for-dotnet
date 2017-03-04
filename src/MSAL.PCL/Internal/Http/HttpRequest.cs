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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Http
{
    internal class HttpRequest
    {
        public static async Task<HttpResponse> SendPost(Uri endpoint, Dictionary<string, string> headers,
            Dictionary<string, string> bodyParameters, RequestContext callstate)
        {
            return
                await
                    ExecuteWithRetry(endpoint, headers, bodyParameters, HttpMethod.Post, callstate)
                        .ConfigureAwait(false);
        }

        public static async Task<HttpResponse> SendGet(Uri endpoint, Dictionary<string, string> headers,
            RequestContext callstate)
        {
            return await ExecuteWithRetry(endpoint, headers, null, HttpMethod.Get, callstate).ConfigureAwait(false);
        }

        private static HttpRequestMessage CreateRequestMessage(Uri endpoint, Dictionary<string, string> headers)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage { RequestUri = endpoint };
            requestMessage.Headers.Accept.Clear();
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            return requestMessage;
        }

        private static async Task<HttpResponse> ExecuteWithRetry(Uri endpoint, Dictionary<string, string> headers,
            Dictionary<string, string> bodyParameters, HttpMethod method,
            RequestContext requestContext, bool retry = true)
        {
            bool isRetryable = false;
            HttpResponse response = null;
            MsalLogger msalLogger = new MsalLogger(requestContext);

            try
            {
                response = await Execute(endpoint, headers, bodyParameters, method);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }

                msalLogger.Info(string.Format(CultureInfo.InvariantCulture,
                        "Response status code does not indicate success: {0} ({1}).",
                        (int)response.StatusCode, response.StatusCode));

                if ((response.StatusCode.Equals(HttpStatusCode.InternalServerError)) ||
                    (response.StatusCode).Equals(HttpStatusCode.GatewayTimeout) ||
                    (response.StatusCode).Equals(HttpStatusCode.ServiceUnavailable))
                {
                    isRetryable = true;
                }
            }
            catch (TaskCanceledException exception)
            {
                msalLogger.Error(exception);
                isRetryable = true;
            }

            if (isRetryable)
            {
                if (retry)
                {
                    msalLogger.Info("Retrying one more time..");
                    return await ExecuteWithRetry(endpoint, headers, bodyParameters, method, requestContext, false);
                }

                msalLogger.Info("Request retry failed.");
                throw new RetryableRequestException();
            }

            return response;
        }

        private static async Task<HttpResponse> Execute(Uri endpoint, Dictionary<string, string> headers,
            Dictionary<string, string> bodyParameters, HttpMethod method)
        {
            HttpClient client = HttpClientFactory.GetHttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using (HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers))
            {
                requestMessage.Method = method;
                if (bodyParameters != null)
                {
                    requestMessage.Content = new FormUrlEncodedContent(bodyParameters);
                }

                using (HttpResponseMessage responseMessage =
                    await client.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    return await CreateResponseAsync(responseMessage).ConfigureAwait(false);
                }
            }
        }

        private static async Task<HttpResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>();
            if (response.Headers != null)
            {
                foreach (var kvp in response.Headers)
                {
                    headers[kvp.Key] = kvp.Value.First();
                }
            }

            return new HttpResponse
            {
                Headers = headers,
                Body = await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                StatusCode = response.StatusCode
            };
        }
    }
}