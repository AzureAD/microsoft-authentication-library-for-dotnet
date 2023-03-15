// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Test.LabInfrastructure;

namespace Microsoft.Identity.Test.Integration.NetFx.Infrastructure
{
    /// <remarks>
    /// We invoke this class from different threads and they all use the same HttpClient.
    /// To prevent race conditions, make sure you do not get / set anything on HttpClient itself,
    /// instead rely on HttpRequest objects which are thread specific.
    ///
    /// In particular, do not change any properties on HttpClient such as BaseAddress, buffer sizes and Timeout. You should
    /// also not access DefaultRequestHeaders because the getters are not thread safe (use HttpRequestMessage.Headers instead).
    /// </remarks>
    internal class MsiProxyHttpManager : IHttpManager
    {
        private readonly string _testWebServiceEndpoint;
        private static HttpClient s_httpClient = new HttpClient();

        public MsiProxyHttpManager(string testWebServiceEndpoint)
        {
            _testWebServiceEndpoint = testWebServiceEndpoint;
        }

        public long LastRequestDurationInMs { get; private set; }

        public Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async Task<HttpResponse> SendGetAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            ILoggerAdapter logger,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(endpoint, headers, null, HttpMethod.Get, logger, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the GET request just like <see cref="SendGetAsync(Uri, IDictionary{string, string}, ILoggerAdapter, bool, CancellationToken)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="HttpResponse"/> associated
        /// with the request.
        /// </summary>
        public async Task<HttpResponse> SendGetForceResponseAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            ILoggerAdapter logger,
            bool retry = true,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(endpoint, headers, null, HttpMethod.Get, logger, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the POST request just like <see cref="SendPostAsync(Uri, IDictionary{string, string}, IDictionary{String, String}, ILoggerAdapter, CancellationToken)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="HttpResponse"/> associated
        /// with the request.
        /// </summary>
        public Task<HttpResponse> SendPostForceResponseAsync(
            Uri uri,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Performs the POST request just like <see cref="SendPostAsync(Uri, IDictionary{string, string}, HttpContent, ILoggerAdapter, CancellationToken)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="HttpResponse"/> associated
        /// with the request.
        /// </summary>
        public Task<HttpResponse> SendPostForceResponseAsync(
            Uri uri,
            IDictionary<string, string> headers,
            StringContent body,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        private async Task<HttpResponse> ExecuteAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            CancellationToken cancellationToken = default)
        {
            //Get token for the MSIHelperService
            var labApi = new LabServiceApi();
            var token = await labApi.GetMSIHelperServiceTokenAsync()
                .ConfigureAwait(false);

            //Add the Authorization header
            s_httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            //encode the URL before sending it to the helper service
            var encodedUri = WebUtility.UrlEncode(endpoint.AbsoluteUri.ToLowerInvariant());

            //http get to the helper service
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_testWebServiceEndpoint + encodedUri));

            //Pass the headers if any to the MSI Helper Service
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            //send the request to the helper service
            HttpResponseMessage result = await s_httpClient.SendAsync(requestMessage)
                .ConfigureAwait(false);

            //Form the response received from the helper service
            HttpResponse response = new HttpResponse()
            {
                StatusCode = result.StatusCode,
                Body = await result.Content.ReadAsStringAsync().ConfigureAwait(false)
            };

            return response;
        }
    }
}
