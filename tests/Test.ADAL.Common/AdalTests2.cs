//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

            MemoryStream stream = new MemoryStream();
            using (var listener = new TextWriterTraceListener(stream))
            {
                Trace.Listeners.Add(listener);

                context.SetCorrelationId(correlationId);
                result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
                VerifySuccessResult(sts, result);
                listener.Flush();
                string trace = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
                Verify.IsTrue(trace.Contains(correlationId.ToString()));
                Trace.Listeners.Remove(listener);
            }

            stream = new MemoryStream();
            using (var listener = new TextWriterTraceListener(stream))
            {
                Trace.Listeners.Add(listener);
                context.SetCorrelationId(Guid.Empty);
                AuthenticationResultProxy result2 = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidClientId);
                Verify.IsNotNullOrEmptyString(result2.AccessToken);
                listener.Flush();
                string trace = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
                Verify.IsFalse(trace.Contains(correlationId.ToString()));
                Verify.IsTrue(trace.Contains("Correlation ID"));
                Trace.Listeners.Remove(listener);
            }
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
                var result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
                AdalTests.VerifySuccessResult(sts, result);

                // ADAL WinRT does not support AuthenticationParameters.CreateFromUnauthorizedResponse API
                if (TestType != Common.TestType.WinRT)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RelyingPartyWithDiscoveryUrl);
                        request.ContentType = "application/x-www-form-urlencoded";
                        response = (HttpWebResponse)request.GetResponse();
                    }
                    catch (WebException ex)
                    {
                        response = (HttpWebResponse)ex.Response;
                        authParams = AuthenticationParametersProxy.CreateFromUnauthorizedResponse(response);
                    }
                    finally
                    {
                        response.Close();
                    }

                    context = new AuthenticationContextProxy(authParams.Authority, sts.ValidateAuthority, TokenCacheType.Null);
                    result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
                    AdalTests.VerifySuccessResult(sts, result);
                }

                authParams = await AuthenticationParametersProxy.CreateFromResourceUrlAsync(new Uri(RelyingPartyWithDiscoveryUrl));
                context = new AuthenticationContextProxy(authParams.Authority, sts.ValidateAuthority, TokenCacheType.Null);
                result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
                AdalTests.VerifySuccessResult(sts, result);

                Log.Comment("Relying Party Terminating...");
            }
        }

        private static void VerifyTokenContent(AuthenticationResultProxy result)
        {

            // Verify the token content confirms the user in AuthenticationResult.UserInfo
            var token = new System.IdentityModel.Tokens.JwtSecurityToken(result.AccessToken);
            foreach (var claim in token.Claims)
            {
                if (claim.Type == "oid")
                {
                    Verify.AreEqual(result.UserInfo.UniqueId, claim.Value);
                }

                if (claim.Type == "upn")
                {
                    Verify.AreEqual(result.UserInfo.DisplayableId, claim.Value);
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
}
