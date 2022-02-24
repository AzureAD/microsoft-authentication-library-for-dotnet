//-----------------------------------------------------------------------
// <copyright file="MainViewController.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Identity.Client;

namespace IntuneMAMSampleiOS
{
    public class RecordingHandler : DelegatingHandler
    {
        private readonly Action<HttpRequestMessage, HttpResponseMessage> _recordingAction;

        public RecordingHandler(Action<HttpRequestMessage, HttpResponseMessage> recordingAction)
        {
            _recordingAction = recordingAction;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _recordingAction.Invoke(request, response);
            return response;
        }
    }

    public class HttpSnifferClientFactory : IMsalHttpClientFactory
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
                System.Diagnostics.Debug.WriteLine($"[MSAL][HTTP Request]: {req}");
                System.Diagnostics.Debug.WriteLine($"[MSAL][HTTP Response]: {res}");
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