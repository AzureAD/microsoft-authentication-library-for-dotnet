// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
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
        // referenced in unit tests, cannot be private
        public const int DEFAULT_ESTS_MAX_RETRIES = 1;
        // this will be overridden in the unit tests so that they run faster
        public static int DEFAULT_ESTS_RETRY_DELAY_MS { get; set; } = 1000;

        protected readonly IMsalHttpClientFactory _httpClientFactory;
        private readonly bool _isManagedIdentity;
        private readonly bool _disableInternalRetries;
        public long LastRequestDurationInMs { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpManager"/> class.
        /// </summary>
        /// <param name="httpClientFactory">
        /// An instance of <see cref="IMsalHttpClientFactory"/> used to create and manage <see cref="HttpClient"/> instances.
        /// This factory ensures proper reuse of <see cref="HttpClient"/> to avoid socket exhaustion.
        /// </param>
        /// <param name="isManagedIdentity">
        /// A boolean flag indicating whether the HTTP manager is being used in a managed identity context.
        /// </param>
        /// <param name="disableInternalRetries">
        /// A boolean flag indicating whether the HTTP manager should enable retry logic for transient failures.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="httpClientFactory"/> is null.
        /// </exception>
        public HttpManager(
            IMsalHttpClientFactory httpClientFactory,
            bool isManagedIdentity,
            bool disableInternalRetries)
        {
            _httpClientFactory = httpClientFactory ??
                throw new ArgumentNullException(nameof(httpClientFactory));
            _isManagedIdentity = isManagedIdentity;
            _disableInternalRetries = disableInternalRetries;
        }

        public async Task<HttpResponse> SendRequestAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            bool doNotThrow,
            X509Certificate2 bindingCertificate,
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert,
            CancellationToken cancellationToken,
            IRetryPolicy retryPolicy = null,
            int retryCount = 0)
        {
            // Use the default STS retry policy if the request is not for managed identity
            // and a non-default STS retry policy is not provided.
            // Skip this if statement the dev indicated that they do not want retry logic.
            if (!_isManagedIdentity && retryPolicy == null && !_disableInternalRetries)
            {
                retryPolicy = new LinearRetryPolicy(
                    DEFAULT_ESTS_RETRY_DELAY_MS,
                    DEFAULT_ESTS_MAX_RETRIES,
                    HttpRetryConditions.Sts);
            }

            Exception timeoutException = null;
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
                    response = await ExecuteAsync(
                        endpoint,
                        headers,
                        clonedBody,
                        method,
                        bindingCertificate,
                        validateServerCert, logger,
                        cancellationToken).ConfigureAwait(false);
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }

                logger.Info(() => string.Format(CultureInfo.InvariantCulture,
                    MsalErrorMessage.HttpRequestUnsuccessful,
                    (int)response.StatusCode, response.StatusCode));
            }
            catch (TaskCanceledException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.Info("The HTTP request was canceled. ");
                    throw;
                }

                logger.Error("The HTTP request failed. " + exception.Message);
                timeoutException = exception;
            }

            while (!_disableInternalRetries && retryPolicy.PauseForRetry(response, timeoutException, retryCount))
            {
                logger.Warning($"Retry condition met. Retry count: {retryCount++} after waiting {retryPolicy.DelayInMilliseconds}ms.");
                return await SendRequestAsync(
                    endpoint,
                    headers,
                    body,
                    method,
                    logger,
                    doNotThrow,
                    bindingCertificate,
                    validateServerCert,
                    cancellationToken,
                    retryPolicy,
                    retryCount) // Pass the updated retry count
                    .ConfigureAwait(false);
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
                string requestUriScrubbed = $"{endpoint.AbsoluteUri.Split('?')[0]}";
                throw MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.ServiceNotAvailable,
                    $"Service is unavailable to process the request. The request Uri is: {requestUriScrubbed} on port {endpoint.Port}",
                    response);
            }

            return response;
        }

        private HttpClient GetHttpClient(X509Certificate2 x509Certificate2, Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert)
        {
            if (x509Certificate2 != null && validateServerCert != null)
            {
                throw new NotImplementedException("Mtls certificate cannot be used with service fabric. A custom http client is used for service fabric managed identity to validate the server certificate.");
            }

            if (validateServerCert != null)
            {
                // If the factory is an IMsalSFHttpClientFactory, use it to get an HttpClient with the custom handler
                // that validates the server certificate.
                if (_httpClientFactory is IMsalSFHttpClientFactory msalSFHttpClientFactory)
                {
                    return msalSFHttpClientFactory.GetHttpClient(validateServerCert);
                }

#if NET471_OR_GREATER || NETSTANDARD || NET
                // If the factory is not an IMsalSFHttpClientFactory, use it to get a default HttpClient
                return new HttpClient(new HttpClientHandler()
                {

                    ServerCertificateCustomValidationCallback = validateServerCert
                });
#else
                return _httpClientFactory.GetHttpClient();
#endif
            }

            if (_httpClientFactory is IMsalMtlsHttpClientFactory msalMtlsHttpClientFactory)
            {
                // If the factory is an IMsalMtlsHttpClientFactory, use it to get an HttpClient with the certificate
                return msalMtlsHttpClientFactory.GetHttpClient(x509Certificate2);
            }

            // If the factory is not an IMsalMtlsHttpClientFactory, use it to get a default HttpClient
            return _httpClientFactory.GetHttpClient();
        }

        private static HttpRequestMessage CreateRequestMessage(Uri endpoint, IDictionary<string, string> headers)
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
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers))
            {
                requestMessage.Method = method;
                requestMessage.Content = body;

                logger.VerbosePii(
                    () => $"[HttpManager] Sending request. Method: {method}. URI: {(endpoint == null ? "NULL" : $"{endpoint.Scheme}://{endpoint.Authority}{endpoint.AbsolutePath}")}. Binding Certificate: {bindingCertificate != null}. Endpoint: {endpoint} ",
                    () => $"[HttpManager] Sending request. Method: {method}. Host: {(endpoint == null ? "NULL" : $"{endpoint.Scheme}://{endpoint.Authority}")}. Binding Certificate: {bindingCertificate != null} ");

                Stopwatch sw = Stopwatch.StartNew();

                HttpClient client = GetHttpClient(bindingCertificate, validateServerCert);

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
