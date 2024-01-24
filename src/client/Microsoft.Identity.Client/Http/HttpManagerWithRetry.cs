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
    internal class HttpManagerWithRetry : HttpManager
    {

        public HttpManagerWithRetry(IMsalHttpClientFactory httpClientFactory) : 
            base(httpClientFactory) { }

        /// <inheritdoc/>
        public override Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            return SendRequestAsync(endpoint, headers, body, HttpMethod.Post, logger, retry: true, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<HttpResponse> SendGetAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            ILoggerAdapter logger,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            return SendRequestAsync(endpoint, headers, null, HttpMethod.Get, logger, retry: true, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<HttpResponse> SendGetForceResponseAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            ILoggerAdapter logger,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            return SendRequestAsync(endpoint, headers, null, HttpMethod.Get, logger, retry: true, doNotThrow: true, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<HttpResponse> SendPostForceResponseAsync(
            Uri uri,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            HttpContent body = bodyParameters == null ? null : new FormUrlEncodedContent(bodyParameters);
            return SendRequestAsync(uri, headers, body, HttpMethod.Post, logger, retry: true, doNotThrow: true, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<HttpResponse> SendPostForceResponseAsync(
            Uri uri,
            IDictionary<string, string> headers,
            StringContent body,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            return SendRequestAsync(uri, headers, body, HttpMethod.Post, logger, retry: true, doNotThrow: true, cancellationToken: cancellationToken);
        }

        protected override HttpClient GetHttpClient()
        {
            return _httpClientFactory.GetHttpClient();
        }

        protected override async Task<HttpResponse> SendRequestAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            bool doNotThrow = false,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            Exception timeoutException = null;
            bool isRetriableStatusCode = false;
            HttpResponse response = null;
            bool isRetriable;
            
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

                logger.Info(() => string.Format(CultureInfo.InvariantCulture,
                    MsalErrorMessage.HttpRequestUnsuccessful,
                    (int)response.StatusCode, response.StatusCode));

                isRetriableStatusCode = IsRetryableStatusCode((int)response.StatusCode);
                isRetriable = isRetriableStatusCode && !HasRetryAfterHeader(response);
            }
            catch (TaskCanceledException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.Info("The HTTP request was cancelled. ");
                    throw;
                }

                logger.Error("The HTTP request failed. " + exception.Message);
                isRetriable = true;
                timeoutException = exception;
            }

            if (isRetriable && retry)
            {
                logger.Info("Retrying one more time..");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                return await SendRequestAsync(
                    endpoint,
                    headers,
                    body,
                    method,
                    logger,
                    doNotThrow,
                    retry: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
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

            // package 500 errors in a "service not available" exception
            if (isRetriableStatusCode)
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
    }
}
