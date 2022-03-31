// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client;

namespace WebApi.MockHttp
{
    /// <summary>
    /// Fakes AAD. Auto-responds to some requests.
    /// </summary>
    public class MockHttpClientFactory : IMsalHttpClientFactory
    {
        readonly HttpClient _httpClient;

        public IList<(HttpRequestMessage, HttpResponseMessage)> RequestsAndResponses { get; }

        public static string LastHttpContentData { get; set; }

        public MockHttpClientFactory()
        {
            RequestsAndResponses = new List<(HttpRequestMessage, HttpResponseMessage)>();

            var recordingHandler = new SelfRespondingHandler((req, res) => {
                if (req.Content != null)
                {
                    req.Content.LoadIntoBufferAsync().GetAwaiter().GetResult();
                    LastHttpContentData = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                RequestsAndResponses.Add((req, res));
                //Trace.WriteLine($"[MSAL][HTTP Request]: {req}");
                //Trace.WriteLine($"[MSAL][HTTP Response]: {res}");
            });
            recordingHandler.InnerHandler = new HttpClientHandler();
            _httpClient = new HttpClient(recordingHandler);
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }
    }

    public class SelfRespondingHandler : DelegatingHandler
    {
        private readonly Action<HttpRequestMessage, HttpResponseMessage> _recordingAction;

        public SelfRespondingHandler(Action<HttpRequestMessage, HttpResponseMessage> recordingAction)
        {
            _recordingAction = recordingAction;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            //HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (request.Method == HttpMethod.Get)
            {
                // discovery call
                if (request.RequestUri.AbsoluteUri.StartsWith(
                    "https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint"))
                {
                    response = MockHttpCreator.CreateInstanceDiscoveryMockHandler();
                }
            }
            else if (request.Method == HttpMethod.Post)
            {
                //await Task.Delay(Settings.NetworkAccessPenaltyMs).ConfigureAwait(false);

                // example endpoint https://login.microsoftonline.com/tid2/oauth2/v2.0/token

                var regexp = @"https://login.microsoftonline.com/(?<tid>.*)/oauth2/v2.0/token"; // captures the tenantID
                var m = Regex.Match(request.RequestUri.AbsoluteUri, regexp);
                var tidGroup = m.Groups["tid"];
                if (tidGroup == null)
                    throw new InvalidOperationException("Should not happen");
                string tid = tidGroup.Value;

                System.Collections.Specialized.NameValueCollection parsedData =
                    await GetRequestPayloadAsync(request).ConfigureAwait(false);

                if (parsedData["grant_type"] == "client_credentials")
                {
                    string fakeSecret = $"access_token_secret_{tid}_{parsedData["scope"]}";
                    response = MockHttpCreator.CreateS2SBearerResponse(fakeSecret);
                }
                else if (parsedData["grant_type"] == "urn:ietf:params:oauth:grant-type:jwt-bearer")
                {
                    string fakeSecret = $"access_token_secret_{tid}_{parsedData["scope"]}";
                    string assertion = parsedData["assertion"];
                    // convention "upstream_token_{user}"
                    var m2 = Regex.Match(assertion, "upstream_token_(?<user>.*)");
                    if (!m2.Success || m2.Groups["user"] == null)
                    {
                        throw new NotSupportedException("Expecting the assertion to be in the format upstream_token_user123");
                    }

                    response = MockHttpCreator.CreateUserTokenResponse(
                        tid, 
                        parsedData["scope"],
                        fakeSecret, 
                        uid: m2.Groups["user"].Value, 
                        utid: tid ); // guests not implemented
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            _recordingAction.Invoke(request, response);
            return response;
        }

        private static async Task<System.Collections.Specialized.NameValueCollection> GetRequestPayloadAsync(HttpRequestMessage request)
        {
            await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);
            var data = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            var parsedData = HttpUtility.ParseQueryString(data);
            return parsedData;
        }
    }
}
