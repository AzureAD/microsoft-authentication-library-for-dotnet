// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common
{
    /// <summary>
    /// Provides an <see cref="HttpClient"/> factory for test scenarios that records HTTP requests and responses,
    /// and supports mutual TLS (mTLS) client certificates. Used for sniffing and asserting HTTP traffic in tests.
    /// </summary>
    public class HttpSnifferClientFactory : IMsalMtlsHttpClientFactory
    {
        readonly HttpClient _httpClient;

        /// <summary>
        /// records all HTTP requests and responses made through clients created by this factory.
        /// </summary>
        public IList<(HttpRequestMessage, HttpResponseMessage)> RequestsAndResponses { get; }

        /// <summary>
        /// last HTTP content data read from a request. This is populated for all requests with content, regardless of whether the test asserts on it, to ensure that tests that do assert on content don't break when this factory is used.
        /// </summary>
        public static string LastHttpContentData { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpSnifferClientFactory"/> class.
        /// Used for recording HTTP requests and responses in test scenarios.
        /// </summary>
        public HttpSnifferClientFactory()
        {
            RequestsAndResponses = new List<(HttpRequestMessage, HttpResponseMessage)>();

            var recordingHandler = new RecordingHandler((req, res) =>
            {
                // Always capture body so tests that assert on it don't break
                if (req.Content != null)
                {
                    req.Content.LoadIntoBufferAsync().GetAwaiter().GetResult();
                    LastHttpContentData = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            
                // Always record so tests can assert on traffic
                RequestsAndResponses.Add((req, res));
            
                // Only print when MSAL_TEST_LOGGING is set
                if (Environment.GetEnvironmentVariable("MSAL_TEST_LOGGING") != null)
                {
                    Trace.WriteLine($"[MSAL][HTTP Request]: {req}");
                    Trace.WriteLine($"[MSAL][HTTP Response]: {res}");
                }
            });

            recordingHandler.InnerHandler = new HttpClientHandler();
            _httpClient = new HttpClient(recordingHandler);
        }

        /// <summary>
        /// Gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }

        /// <summary>
        /// Gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations with a client certificate.
        /// </summary>
        /// <param name="x509Certificate2">The client certificate to use for mutual TLS (mTLS) authentication.</param>
        /// <returns></returns>
        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                return GetHttpClient();
            }

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(x509Certificate2);

            return new HttpClient(handler);
        }
    }
}
