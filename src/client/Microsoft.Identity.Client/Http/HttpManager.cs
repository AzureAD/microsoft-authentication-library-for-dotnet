// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    /// <remarks>
    /// We invoke this class from different threads and they all use the same HttpClient.
    /// To prevent race conditions, make sure you do not get / set anything on HttpClient itself,
    /// instead rely on HttpRequest objects which are thread specific.
    ///
    /// In particular, do not change any properties on HttpClient such as BaseAddress, buffer sizes and Timeout. You should
    /// also not access DefaultRequestHeaders because the getters are not thread safe (use HttpRequestMessage.Headers instead).
    /// </remarks>
    internal class HttpManager : IHttpManager
    {
        private readonly IMsalHttpClientFactory _httpClientFactory;
        public long LastRequestDurationInMs { get; private set; }

        public HttpManager(IMsalHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? 
                throw new ArgumentNullException(nameof(httpClientFactory));
        }

        protected virtual HttpClient GetHttpClient()
        {
            return _httpClientFactory.GetHttpClient();
        }

        public async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            ICoreLogger logger,
            CancellationToken cancellationToken = default)
        {
            HttpContent body = bodyParameters == null ? null : new FormUrlEncodedContent(bodyParameters);
            return await SendPostAsync(endpoint, headers, body, logger, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            ICoreLogger logger,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(endpoint, headers, body, HttpMethod.Post, logger, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendGetAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            ICoreLogger logger,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(endpoint, headers, null, HttpMethod.Get, logger, retry: retry, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the POST request just like <see cref="SendPostAsync(Uri, IDictionary{string, string}, HttpContent, ICoreLogger, CancellationToken)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="HttpResponse"/> associated
        /// with the request.
        /// </summary>
        public async Task<HttpResponse> SendPostForceResponseAsync(
            Uri uri,
            Dictionary<string, string> headers,
            StringContent body,
            ICoreLogger logger,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(uri, headers, body, HttpMethod.Post, logger, doNotThrow: true, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            ICoreLogger logger,
            bool doNotThrow = false,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            Exception timeoutException = null;
            bool isRetryable = false;
            bool is5xxError = false;
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

                using (logger.LogBlockDuration("[HttpManager] ExecuteAsync"))
                {
                    response = await ExecuteAsync(endpoint, headers, clonedBody, method, logger, cancellationToken).ConfigureAwait(false);
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }

                logger.Info(string.Format(CultureInfo.InvariantCulture,
                    MsalErrorMessage.HttpRequestUnsuccessful,
                    (int)response.StatusCode, response.StatusCode));

                is5xxError = (int)response.StatusCode >= 500 && (int)response.StatusCode < 600;
                isRetryable = is5xxError && !HasRetryAfterHeader(response);
            }
            catch (TaskCanceledException exception)
            {
                logger.Error("The HTTP request failed or it was canceled. " + exception.Message);
                isRetryable = true;

                if (cancellationToken.IsCancellationRequested)
                {
                    isRetryable = false;
                }

                timeoutException = exception;
            }

            if (isRetryable && retry)
            {

                logger.Info("Retrying one more time..");
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                return await ExecuteWithRetryAsync(
                    endpoint,
                    headers,
                    body,
                    method,
                    logger,
                    doNotThrow,
                    retry: false).ConfigureAwait(false);
            }

            logger.Warning("Request retry failed.");
            if (timeoutException != null)
            {
                throw new MsalServiceException(
                    MsalError.RequestTimeout,
                    "Request to the endpoint timed out.",
                    timeoutException);
            }

            if (doNotThrow)
            {
                return response;
            }

            if (is5xxError)
            {
                throw MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.ServiceNotAvailable,
                    "Service is unavailable to process the request",
                    response);
            }

            return response;
        }

        private static bool HasRetryAfterHeader(HttpResponse response)
        {
            var retryAfter = response?.Headers?.RetryAfter;
            return retryAfter != null &&
                (retryAfter.Delta.HasValue || retryAfter.Date.HasValue);
        }

        private async Task<HttpResponse> ExecuteAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ICoreLogger logger,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers))
            {
                requestMessage.Method = method;
                requestMessage.Content = body;

                logger.VerbosePii(
                    $"[HttpManager] Sending request. Method: {method}. URI: {(endpoint == null ? "NULL" : $"{endpoint.Scheme}://{endpoint.Authority}{endpoint.AbsolutePath}")}. ",
                    $"[HttpManager] Sending request. Method: {method}. Host: {(endpoint == null ? "NULL" : $"{endpoint.Scheme}://{endpoint.Authority}")}. ");

                Stopwatch sw = Stopwatch.StartNew();

                HttpClient client = GetHttpClient();

                using (HttpResponseMessage responseMessage =
                    await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false))
                {
                    LastRequestDurationInMs = sw.ElapsedMilliseconds;
                    logger.Verbose($"[HttpManager] Received response. Status code: {responseMessage.StatusCode}. ");

                    HttpResponse returnValue = await CreateResponseAsync(responseMessage).ConfigureAwait(false);
                    returnValue.UserAgent = requestMessage.Headers.UserAgent.ToString();
                    return returnValue;
                }
            }
        }

        internal /* internal for test only */ static async Task<HttpResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var body = response.Content == null
                           ? null
                           : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponse
            {
                Headers = response.Headers,
                Body = body,
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
            // On UWP there is a bug in the HTTP stack that causes an exception to be thrown when moving around a stream.
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
