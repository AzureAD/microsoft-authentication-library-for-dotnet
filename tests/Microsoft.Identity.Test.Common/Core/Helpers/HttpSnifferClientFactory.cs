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
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

                // check the .net runtime
                var framework = RuntimeInformation.FrameworkDescription;

                // This will match ".NET 5.0", ".NET 6.0", ".NET 7.0", ".NET 8.0", etc.
                if (framework.StartsWith(".NET ", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the version number
                    var versionString = framework.Substring(5).Trim(); // e.g., "6.0.0"
                    if (Version.TryParse(versionString, out var version) && version.Major >= 5)
                    {
                        Assert.AreEqual(new Version(2, 0), req.Version, $"Request version mismatch: {req.Version}. MSAL on NET 5+ expects HTTP/2.0 for all requests.");
                        // ESTS-R endpoint does not support HTTP/2.0, so we don't assert this
                    }
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
                return GetHttpClient();
            }

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(x509Certificate2);

            return new HttpClient(handler);
        }
    }
}
