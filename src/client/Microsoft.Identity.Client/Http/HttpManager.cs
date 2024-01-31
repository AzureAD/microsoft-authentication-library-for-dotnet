// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
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
        protected readonly IMsalHttpClientFactory _httpClientFactory;
        private readonly Func<HttpResponse, bool> _retryCondition;

        public long LastRequestDurationInMs { get; private set; }

        /// <summary>
        /// A new instance of the HTTP manager with a retry *condition*. The retry policy hardcodes: 
        /// - the number of retries (1)
        /// - the delay between retries (1 second)
        /// </summary>
        public HttpManager(
            IMsalHttpClientFactory httpClientFactory, 
            Func<HttpResponse, bool> retryCondition)
        {
            _httpClientFactory = httpClientFactory ??
                throw new ArgumentNullException(nameof(httpClientFactory));
            _retryCondition = retryCondition;
        }

      

        public async Task<HttpResponse> SendRequestAsync(
            Uri endpoint,
            Dictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            bool doNotThrow,
            bool retry,
            X509Certificate2 bindingCertificate,
            CancellationToken cancellationToken)
        {
            Exception timeoutException = null;
            HttpResponse response = null;
            bool isRetriable = false;

            try
            {
                //HttpContent body = bodyParameters == null ? null : new FormUrlEncodedContent(bodyParameters);

                HttpContent clonedBody = body;
                if (body != null)
                {
                    // Since HttpContent would be disposed by underlying client.SendAsync(),
                    // we duplicate it so that we will have a copy in case we would need to retry
                    clonedBody = await CloneHttpContentAsync(body).ConfigureAwait(false);
                }

                using (logger.LogBlockDuration("[HttpManager] ExecuteAsync"))
                {
                    response = await ExecuteAsync(
                        endpoint, 
                        headers, 
                        clonedBody, 
                        method, 
                        bindingCertificate, 
                        logger, 
                        cancellationToken).ConfigureAwait(false);
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }

                logger.Info(() => string.Format(CultureInfo.InvariantCulture,
                    MsalErrorMessage.HttpRequestUnsuccessful,
                    (int)response.StatusCode, response.StatusCode));

                isRetriable = _retryCondition(response);
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
                logger.Warning("Retry condition met. Retrying 1 time after waiting 1 second.");
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                return await SendRequestAsync(
                    endpoint,
                    headers,
                    body,
                    method,
                    logger,
                    doNotThrow,
                    retry: false,  // retry just once
                    bindingCertificate,
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
            if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
            {
                throw MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.ServiceNotAvailable,
                    "Service is unavailable to process the request",
                    response);
            }

            return response;
        }

        private HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (_httpClientFactory is IMsalMtlsHttpClientFactory msalMtlsHttpClientFactory)
            {
                // If the factory is an IMsalMtlsHttpClientFactory, use it to get an HttpClient with the certificate
                return msalMtlsHttpClientFactory.GetHttpClient(x509Certificate2);
            }

            // If the factory is not an IMsalMtlsHttpClientFactory, use it to get a default HttpClient
            return _httpClientFactory.GetHttpClient();
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

        private async Task<HttpResponse> ExecuteAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            X509Certificate2 bindingCertificate,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers))
            {
                requestMessage.Method = method;
                requestMessage.Content = body;

                logger.VerbosePii(
                    () => $"[HttpManager] Sending request. Method: {method}. URI: {(endpoint == null ? "NULL" : $"{endpoint.Scheme}://{endpoint.Authority}{endpoint.AbsolutePath}")}. Binding Certificate: {bindingCertificate != null} ",
                    () => $"[HttpManager] Sending request. Method: {method}. Host: {(endpoint == null ? "NULL" : $"{endpoint.Scheme}://{endpoint.Authority}")}. Binding Certificate: {bindingCertificate != null} ");

                Stopwatch sw = Stopwatch.StartNew();

                HttpClient client = GetHttpClient(bindingCertificate);

                using (HttpResponseMessage responseMessage =
                    await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false))
                {
                    LastRequestDurationInMs = sw.ElapsedMilliseconds;
                    logger.Verbose(() => $"[HttpManager] Received response. Status code: {responseMessage.StatusCode}. ");

                    HttpResponse returnValue = await CreateResponseAsync(responseMessage).ConfigureAwait(false);
                    returnValue.UserAgent = requestMessage.Headers.UserAgent.ToString();
                    return returnValue;
                }
            }
        }

        protected static async Task<HttpContent> CloneHttpContentAsync(HttpContent httpContent)
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

        
        

        #region Helpers
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

     



        #endregion
    }
}
