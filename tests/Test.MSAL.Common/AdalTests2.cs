//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;

namespace Test.ADAL.Common
{
    internal partial class AdalTests
    {
        public static async Task CorrelationIdTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            Guid correlationId = Guid.NewGuid();
            AuthenticationResultProxy result = null;

            var eventListener = new SampleEventListener();
            eventListener.EnableEvents(AdalOption.AdalEventSource, EventLevel.Verbose);

            context.SetCorrelationId(correlationId);
            result = await context.AcquireTokenAsync(sts.ValidScope, sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            Verify.IsTrue(eventListener.TraceBuffer.Contains(correlationId.ToString()));

            eventListener.TraceBuffer = string.Empty;

            context.SetCorrelationId(Guid.Empty);
            AuthenticationResultProxy result2 = await context.AcquireTokenSilentAsync(sts.ValidScope, sts.ValidClientId);
            Verify.IsNotNullOrEmptyString(result2.AccessToken);
            Verify.IsFalse(eventListener.TraceBuffer.Contains(correlationId.ToString()));
        }

        public static async Task AuthenticationParametersDiscoveryTestAsync(Sts sts)
        {
            const string RelyingPartyWithDiscoveryUrl = "http://localhost:8080";

            using (Microsoft.Owin.Hosting.WebApp.Start<RelyingParty>(RelyingPartyWithDiscoveryUrl))
            {
                Log.Comment("Relying Party Started");

                HttpWebResponse response = null;
                AuthenticationParametersProxy authParams = null;

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RelyingPartyWithDiscoveryUrl);
                    request.ContentType = "application/x-www-form-urlencoded";
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    response = (HttpWebResponse)ex.Response;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        authParams = AuthenticationParametersProxy.CreateFromResponseAuthenticateHeader(response.Headers["WWW-authenticate"]);
                    }
                }
                finally
                {
                    response.Close();
                }

                SetCredential(sts);
                var context = new AuthenticationContextProxy(authParams.Authority, sts.ValidateAuthority, TokenCacheType.Null);
                var result = await context.AcquireTokenAsync(sts.ValidScope, sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
                VerifySuccessResult(sts, result);

                // ADAL WinRT does not support AuthenticationParameters.CreateFromUnauthorizedResponse API
                if (TestType != TestType.WinRT)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        HttpResponseMessage responseMessage = await client.GetAsync(RelyingPartyWithDiscoveryUrl);
                        authParams = await AuthenticationParametersProxy.CreateFromUnauthorizedResponseAsync(responseMessage);
                    }

                    context = new AuthenticationContextProxy(authParams.Authority, sts.ValidateAuthority, TokenCacheType.Null);
                    result = await context.AcquireTokenAsync(sts.ValidScope, sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
                    VerifySuccessResult(sts, result);
                }

                authParams = await AuthenticationParametersProxy.CreateFromResourceUrlAsync(new Uri(RelyingPartyWithDiscoveryUrl));
                context = new AuthenticationContextProxy(authParams.Authority, sts.ValidateAuthority, TokenCacheType.Null);
                result = await context.AcquireTokenAsync(sts.ValidScope, sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
                AdalTests.VerifySuccessResult(sts, result);

                Log.Comment("Relying Party Terminating...");
            }
        }

        private static void VerifyTokenContent(AuthenticationResultProxy result)
        {

            // Verify the token content confirms the user in AuthenticationResult.User
            var token = new System.IdentityModel.Tokens.JwtSecurityToken(result.AccessToken);
            foreach (var claim in token.Claims)
            {
                if (claim.Type == "oid")
                {
                    Verify.AreEqual(result.User.UniqueId, claim.Value);
                }

                if (claim.Type == "upn")
                {
                    Verify.AreEqual(result.User.DisplayableId, claim.Value);
                }
            }
        }

        private static void VerifySuccessResultAndTokenContent(Sts sts, AuthenticationResultProxy result, bool supportRefreshToken = true, bool supportUserInfo = true)
        {
            VerifySuccessResult(sts, result, supportRefreshToken, supportUserInfo);
            if (supportUserInfo)
            {
                VerifyTokenContent(result);
            }
        }
    }

    class SampleEventListener : EventListener
    {
        public string TraceBuffer { get; set; }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            TraceBuffer += (eventData.Payload[0] + "\n");
        }
    }
}
