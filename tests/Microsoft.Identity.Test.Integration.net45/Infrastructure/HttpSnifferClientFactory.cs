// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Integration.net45.Infrastructure
{
    public class HttpSnifferClientFactory : IMsalHttpClientFactory
    {
        HttpClient _httpClient;

        public IList<(HttpRequestMessage, HttpResponseMessage)> RequestsAndResponses { get; }

        public HttpSnifferClientFactory()
        {
            RequestsAndResponses = new List<(HttpRequestMessage, HttpResponseMessage)>();

            var recordingHandler = new RecordingHandler((req, res) => RequestsAndResponses.Add((req, res)));
            recordingHandler.InnerHandler = new HttpClientHandler();
            _httpClient = new HttpClient(recordingHandler);
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }
    }
}
