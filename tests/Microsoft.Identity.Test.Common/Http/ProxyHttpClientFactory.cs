// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Text;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common.Http
{
    public class RemoteHttpFactory : IMsalHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public RemoteHttpFactory(Uri proxyUri)
        {

            _httpClient = new HttpClient(new RemoteServiceHostHandler(proxyUri));
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }
    }
}
