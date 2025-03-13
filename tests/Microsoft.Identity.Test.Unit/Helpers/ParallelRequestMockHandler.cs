// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.RequestsTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.Helpers
{
    /// <summary>
    /// This custom HttpManager does the following: 
    /// - responds to instance discovery calls
    /// - responds with valid token response based on a naming convention (uid = "uid" + rtSecret, upn = "user_" + rtSecret) for "refresh_token" flow
    /// - responds with valid app token response for client_credentials flow.    
    /// </summary>
    internal class ParallelRequestMockHandler : IHttpManager
    {
        public long LastRequestDurationInMs => 5;

        public async Task<HttpResponse> SendRequestAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            bool doNotThrow,
            X509Certificate2 mtlsCertificate,
            HttpClient customHttpClient,
            CancellationToken cancellationToken,
            int retryCount = 0)
        {
            // simulate delay and also add complexity due to thread context switch
            await Task.Delay(ParallelRequestsTests.NetworkAccessPenaltyMs).ConfigureAwait(false);

            if (HttpMethod.Get == method &&
                endpoint.AbsoluteUri.StartsWith("https://login.microsoftonline.com/common/discovery/instance?api-version=1.1"))
            {
                return new HttpResponse()
                {
                    Body = TestConstants.DiscoveryJsonResponse,
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }

            if (HttpMethod.Post == method &&
                UriWithoutQuery(endpoint).AbsoluteUri.EndsWith("oauth2/v2.0/token"))
            {
                var bodyString = await (body as FormUrlEncodedContent).ReadAsStringAsync().ConfigureAwait(false);
                var bodyDict = bodyString.Replace("?", "").Split('&').ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

                if (bodyDict["grant_type"] == "refresh_token")
                {
                    bodyDict.TryGetValue(OAuth2Parameter.RefreshToken, out string rtSecret);

                    return new HttpResponse()
                    {
                        Body = GetTokenResponseForRt(rtSecret),
                        StatusCode = System.Net.HttpStatusCode.OK
                    };
                }

                if (bodyDict["grant_type"] == "client_credentials")
                {
                    var segments = endpoint.AbsolutePath.Split('/');
                    string tid = segments[1];

                    HttpResponseMessage response =
                        MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage($"token_{tid}");
                    string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return new HttpResponse()
                    {
                        Body = payload,
                        StatusCode = System.Net.HttpStatusCode.OK
                    };
                }
            }

            Assert.Fail("Test issue - this HttpRequest is not mocked");
            return null;
        }

        private Uri UriWithoutQuery(Uri uri)
        {
            return new UriBuilder(uri) { Query = string.Empty }.Uri;
        }

        private string GetTokenResponseForRt(string rtSecret)
        {
            if (int.TryParse(rtSecret, out int i))
            {
                var upn = ParallelRequestsTests.GetUpn(i);
                var uid = ParallelRequestsTests.GetUid(i);
                HttpResponseMessage response = MockHelpers.CreateSuccessTokenResponseMessageWithUid(uid, TestConstants.Utid, upn);
                return response.Content.ReadAsStringAsync().Result;
            }

            Assert.Fail("Expecting the rt secret to be a number, to be able to craft a response");
            return null;
        }
    }
}
