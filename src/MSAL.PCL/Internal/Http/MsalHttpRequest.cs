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
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Http
{
    internal class MsalHttpRequest
    {
        private MsalHttpRequest()
        {
        }

        public static async Task<MsalHttpResponse> SendPost(Uri endpoint, Dictionary<string, string> headers,
            Dictionary<string, string> bodyParameters, CallState callstate)
        {
            HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers);
            requestMessage.Content = new FormUrlEncodedContent(bodyParameters);
            requestMessage.Method = HttpMethod.Post;

            return await Execute(requestMessage, callstate).ConfigureAwait(false);
        }

        public static async Task<MsalHttpResponse> SendGet(Uri endpoint, Dictionary<string, string> headers,
            CallState callstate)
        {
            HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers);
            requestMessage.Method = HttpMethod.Get;

            return await Execute(requestMessage, callstate).ConfigureAwait(false);
        }

        private static HttpRequestMessage CreateRequestMessage(Uri endpoint, Dictionary<string, string> headers)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage {RequestUri = endpoint};
            requestMessage.Headers.Accept.Clear();

            foreach (KeyValuePair<string, string> kvp in headers)
            {
                requestMessage.Headers.Add(kvp.Key, kvp.Value);
            }

            return requestMessage;
        }

        private static async Task<MsalHttpResponse> Execute(HttpRequestMessage requestMessage, CallState callstate,
            bool canRetry = true)
        {
            HttpClient client = HttpClientFactory.GetHttpClient();
            bool isRetryable = false;
            MsalHttpResponse response = null;

            using (requestMessage)
            {
                try
                {
                    using (
                        HttpResponseMessage responseMessage =
                            await client.SendAsync(requestMessage).ConfigureAwait(false))
                    {
                        response = await CreateResponseAsync(responseMessage).ConfigureAwait(false);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return response;
                        }

                        PlatformPlugin.Logger.Error(callstate,
                            string.Format(CultureInfo.InvariantCulture,
                                "Response status code does not indicate success: {0} ({1}).",
                                (int) response.StatusCode, response.StatusCode));

                        if ((response.StatusCode.Equals(HttpStatusCode.InternalServerError)) ||
                            (response.StatusCode).Equals(HttpStatusCode.GatewayTimeout) ||
                            (response.StatusCode).Equals(HttpStatusCode.ServiceUnavailable))
                        {
                            isRetryable = true;
                        }
                    }
                }
                catch (TaskCanceledException exception)
                {
                    PlatformPlugin.Logger.Error(callstate, exception);
                    isRetryable = true;
                }

                if (isRetryable)
                {
                    if (canRetry)
                    {
                        PlatformPlugin.Logger.Information(callstate, "Retrying one more time..");
                        return await Execute(requestMessage, callstate, false).ConfigureAwait(false);
                    }

                    PlatformPlugin.Logger.Information(callstate,
                        "Request retry failed.");
                    throw new RetryableRequestException();
                }

                return response;
            }
        }

        private static async Task<MsalHttpResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>();
            if (response.Headers != null)
            {
                foreach (var kvp in response.Headers)
                {
                    headers[kvp.Key] = kvp.Value.First();
                }
            }

            return new MsalHttpResponse
            {
                Headers = headers,
                Body = await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                StatusCode = response.StatusCode
            };
        }
    }
}