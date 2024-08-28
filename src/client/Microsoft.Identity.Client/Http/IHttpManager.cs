// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Http
{
    internal interface IHttpManager
    {
        long LastRequestDurationInMs { get; }

        /// <summary>
        /// Method to send a request to the server using the HttpClient configured in the implementation.
        /// </summary>
        /// <param name="endpoint">The endpoint to send the request to.</param>
        /// <param name="headers">Headers to send in the http request.</param>
        /// <param name="body">Body of the request.</param>
        /// <param name="method">Http method.</param>
        /// <param name="logger">Logger from the request context.</param>
        /// <param name="doNotThrow">Flag to decide if MsalServiceException is thrown or the response is returned in case of 5xx errors.</param>
        /// <param name="mtlsCertificate">Certificate used for MTLS authentication.</param>
        /// <param name="customHttpClient">Custom http client which bypasses the HttpClientFactory. 
        /// This is needed for service fabric managed identity where a cert validation callback is added to the handler.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="retryCount">Number of retries to be attempted in case of retriable status codes.</param>
        /// <returns></returns>
        Task<HttpResponse> SendRequestAsync(
           Uri endpoint,
           IDictionary<string, string> headers,
           HttpContent body,
           HttpMethod method,
           ILoggerAdapter logger,
           bool doNotThrow,
           X509Certificate2 mtlsCertificate,
           HttpClient customHttpClient,
           CancellationToken cancellationToken,
           int retryCount = 0);
    }
}
