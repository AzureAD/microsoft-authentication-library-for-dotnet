// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common
{
    public class HttpSnifferClientFactory : IMsalMtlsHttpClientFactory
    {
        readonly HttpClient _httpClient;

        public IList<(HttpRequestMessage, HttpResponseMessage)> RequestsAndResponses { get; }

        public static string LastHttpContentData { get; set; }

        public HttpSnifferClientFactory()
        {
            RequestsAndResponses = new List<(HttpRequestMessage, HttpResponseMessage)>();

            var recordingHandler = new RecordingHandler((req, res) => {
                if (req.Content != null)
                {
                    req.Content.LoadIntoBufferAsync().GetAwaiter().GetResult();
                    LastHttpContentData = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
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

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                throw new ArgumentNullException(nameof(x509Certificate2), "Certificate cannot be null for MTLS.");
            }

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(x509Certificate2);

            return new HttpClient(handler);
        }
    }
}
