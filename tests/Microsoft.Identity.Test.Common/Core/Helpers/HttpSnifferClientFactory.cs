// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common
{
    public class HttpSnifferClientFactory : IMsalHttpClientFactory
    {
        readonly HttpClient _httpClient;

        public IList<(HttpRequestMessage, HttpResponseMessage)> RequestsAndResponses { get; }

        public HttpSnifferClientFactory()
        {
            RequestsAndResponses = new List<(HttpRequestMessage, HttpResponseMessage)>();

            var recordingHandler = new RecordingHandler((req, res) => { 
                RequestsAndResponses.Add((req, res));
                Trace.WriteLine($"[MSAL][HTTP Request]: {req}");
                Trace.WriteLine($"[MSAL][HTTP Response]: {res}");
            });
            recordingHandler.InnerHandler = new HttpClientHandler();
            _httpClient = new HttpClient(recordingHandler);
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }
    }
}
