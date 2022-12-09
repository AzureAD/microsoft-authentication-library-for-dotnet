// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Microsoft.Identity.Test.Common.Http
{
    public class RemoteServiceHostHandler : DelegatingHandler
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly Uri _proxyUri;

        public RemoteServiceHostHandler(Uri proxyUri)
        {
            _proxyUri = proxyUri;
        }


        /// <summary>
        /// Stop the request. Instead, call the MSI helper.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // HttpRequestMessage is not serializable, so we need to create a facade
            HttpRequestFacade requestFacade = HttpRequestFacade.FromHttpRequestMessage(request);
            

            // Send the request to the helper service
            var response = await _httpClient.PostAsync(_proxyUri, requestFacade.ToFormUrlEncodedContent())
                .ConfigureAwait(false);

            return response;
        }
    }
}
