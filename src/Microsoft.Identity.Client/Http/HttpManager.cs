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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;

namespace Microsoft.Identity.Client.Http
{
    internal class HttpManager : IHttpManager
    {
        private readonly ICoreExceptionFactory _coreExceptionFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpManager(ICoreExceptionFactory coreExceptionFactory, IHttpClientFactory httpClientFactory = null)
        {
            _coreExceptionFactory = coreExceptionFactory ?? CoreExceptionFactory.Instance;
            _httpClientFactory = httpClientFactory ?? new HttpClientFactory();
        }

        protected virtual HttpClient GetHttpClient()
        {
            return _httpClientFactory.HttpClient;
        }

        public async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            RequestContext requestContext)
        {
            HttpContent body = bodyParameters == null ? null : new FormUrlEncodedContent(bodyParameters);
            return await SendPostAsync(endpoint, headers, body, requestContext).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendPostAsync(
            Uri endpoint, 
            IDictionary<string, string> headers,
            HttpContent body, 
            RequestContext requestContext)
        {
            return await ExecuteWithRetryAsync(endpoint, headers, body, HttpMethod.Post, requestContext).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendGetAsync(
            Uri endpoint,
            Dictionary<string, string> headers,
            RequestContext requestContext)
        {
            return await ExecuteWithRetryAsync(endpoint, headers, null, HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the POST request just like <see cref="SendPostAsync(Uri, IDictionary{string, string}, HttpContent, RequestContext)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="IHttpWebResponse"/> associated
        /// with the request.
        /// </summary>
        public async Task<IHttpWebResponse> SendPostForceResponseAsync(
            Uri uri,
            Dictionary<string, string> headers,
            StringContent body,
            RequestContext requestContext)
        {
            return await ExecuteWithRetryAsync(uri, headers, body, HttpMethod.Post, requestContext, doNotThrow: true).ConfigureAwait(false);
        }

        private HttpRequestMessage CreateRequestMessage(Uri endpoint, IDictionary<string, string> headers)
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

        private async Task<HttpResponse> ExecuteWithRetryAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            RequestContext requestContext,
            bool doNotThrow = false,
            bool retry = true)
        {
            Exception timeoutException = null;
            bool isRetryable = false;
            HttpResponse response = null;

            try
            {
                HttpContent clonedBody = body;
                if (body != null)
                {
                    // Since HttpContent would be disposed by underlying client.SendAsync(),
                    // we duplicate it so that we will have a copy in case we would need to retry
                    clonedBody = await CloneHttpContentAsync(body).ConfigureAwait(false);
                }

                response = await ExecuteAsync(endpoint, headers, clonedBody, method).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }

                requestContext.Logger.Info(string.Format(CultureInfo.InvariantCulture,
                    CoreErrorMessages.HttpRequestUnsuccessful,
                    (int)response.StatusCode, response.StatusCode));

                if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
                {
                    isRetryable = true;
                }
            }
            catch (TaskCanceledException exception)
            {
                requestContext.Logger.ErrorPii(exception);
                isRetryable = true;
                timeoutException = exception;
            }

            if (isRetryable)
            {
                if (retry)
                {
                    requestContext.Logger.Info("Retrying one more time..");
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    return await ExecuteWithRetryAsync(
                        endpoint,
                        headers,
                        body,
                        method,
                        requestContext,
                        doNotThrow,
                        retry: false).ConfigureAwait(false);
                }

                requestContext.Logger.Info("Request retry failed.");
                if (timeoutException != null)
                {
                    throw _coreExceptionFactory.GetServiceException(
                        CoreErrorCodes.RequestTimeout,
                        "Request to the endpoint timed out.",
                        timeoutException, 
                        null); // no http response to add more details to this exception
                }

                if (doNotThrow)
                {
                    return response;
                }

                throw _coreExceptionFactory.GetServiceException(
                        CoreErrorCodes.ServiceNotAvailable,
                        "Service is unavailable to process the request",
                        response);
            }

            return response;
        }

        private async Task<HttpResponse> ExecuteAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method)
        {
            HttpClient client = GetHttpClient();

            using (HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers))
            {
                requestMessage.Method = method;
                requestMessage.Content = body;

                using (HttpResponseMessage responseMessage =
                    await client.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    HttpResponse returnValue = await CreateResponseAsync(responseMessage).ConfigureAwait(false);
                    returnValue.UserAgent = client.DefaultRequestHeaders.UserAgent.ToString();
                    return returnValue;
                }
            }
        }

        internal /* internal for test only */ static async Task<HttpResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            return new HttpResponse
            {
                Headers = response.Headers,
                Body = await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                StatusCode = response.StatusCode
            };
        }

        private async Task<HttpContent> CloneHttpContentAsync(HttpContent httpContent)
        {
            var temp = new MemoryStream();
            await httpContent.CopyToAsync(temp).ConfigureAwait(false);
            temp.Position = 0;

            var clone = new StreamContent(temp);
            if (httpContent.Headers != null)
            {
                foreach (var h in httpContent.Headers)
                {
                    clone.Headers.Add(h.Key, h.Value);
                }
            }

#if WINDOWS_APP
            // WORKAROUND 
            // On UWP there is a bug in the Http stack that causes an exception to be thrown when moving around a stream.
            // https://stackoverflow.com/questions/31774058/postasync-throwing-irandomaccessstream-error-when-targeting-windows-10-uwp
            // LoadIntoBufferAsync is necessary to buffer content for multiple reads - see https://stackoverflow.com/questions/26942514/multiple-calls-to-httpcontent-readasasync
            // Documentation is sparse, but it looks like loading the buffer into memory avoids the bug, without 
            // replacing the System.Net.HttpClient with Windows.Web.Http.HttpClient, which is not exactly a drop in replacement
            await clone.LoadIntoBufferAsync().ConfigureAwait(false);
#endif

            return clone;
        }
    }
}
