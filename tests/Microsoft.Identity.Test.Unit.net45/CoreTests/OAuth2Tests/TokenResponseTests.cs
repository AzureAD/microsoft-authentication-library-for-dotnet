// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.OAuth2Tests
{
    [TestClass]
    public class TokenResponseTests : TestBase
    {
        [TestMethod]
        public void ExpirationTimeTest()
        {
            // Need to get timestamp here since it needs to be before we create the token.
            // ExpireOn time is calculated from UtcNow when the object is created.
            DateTimeOffset current = DateTimeOffset.UtcNow;
            const long ExpiresInSeconds = 3599;

            var response = TestConstants.CreateMsalTokenResponse();

            Assert.IsTrue(response.AccessTokenExpiresOn.Subtract(current) >= TimeSpan.FromSeconds(ExpiresInSeconds));
        }

        [TestMethod]
        public void JsonDeserializationTest()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.DefaultLogger, harness.HttpManager, new TelemetryManager(
                    harness.ServiceBundle.Config,
                    harness.ServiceBundle.PlatformProxy,
                    null));

                Task<MsalTokenResponse> task = client.GetTokenAsync(
                    new Uri(TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid()));
                MsalTokenResponse response = task.Result;
                Assert.IsNotNull(response);
            }
        }

        [TestMethod]
        public void BrokerTokenResponse()
        {
            //            string s2 = @"{""access_token"":""atsecret"",
            //""authority"":""https://login.microsoftonline.com/common"",""cached_at"":1591266887,""client_id"":""4a1aa1d5-c567-49d0-ad0b-cd957a47f842"",""client_info"":""eyJ1aWQiOiJhZTgyMWU0ZC1mNDA4LTQ1MWEtYWY4Mi04ODI2OTExNDg2MDMiLCJ1dGlkIjoiNDlmNTQ4ZDAtMTJiNy00MTY5LWEzOTAtYmI1MzA0ZDI0NDYyIn0"",""environment"":""login.windows.net"",""expires_on"":1591270486,""ext_expires_on"":1591270486,""home_account_id"":""ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462"",""http_response_code"":0,""id_token"":""eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IlNzWnNCTmhaY0YzUTlTNHRycFFCVEJ5TlJSSSJ9.eyJhdWQiOiI0YTFhYTFkNS1jNTY3LTQ5ZDAtYWQwYi1jZDk1N2E0N2Y4NDIiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNDlmNTQ4ZDAtMTJiNy00MTY5LWEzOTAtYmI1MzA0ZDI0NDYyL3YyLjAiLCJpYXQiOjE1OTEyNjY1ODcsIm5iZiI6MTU5MTI2NjU4NywiZXhwIjoxNTkxMjcwNDg3LCJuYW1lIjoiTGl1IEthbmciLCJvaWQiOiJhZTgyMWU0ZC1mNDA4LTQ1MWEtYWY4Mi04ODI2OTExNDg2MDMiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJsaXUua2FuZ0Bib2dhdnJpbExURC5vbm1pY3Jvc29mdC5jb20iLCJyaCI6IjAuQVNrQTBFajFTYmNTYVVHamtMdFRCTkpFWXRXaEdrcG54ZEJKclF2TmxYcEgtRUlwQUE4LiIsInN1YiI6IkRfaGFOUXlsVzgzQTV3MlV0STk1clg3NDhtZHhLTlk3MkJHZ2Y4M21JN1kiLCJ0aWQiOiI0OWY1NDhkMC0xMmI3LTQxNjktYTM5MC1iYjUzMDRkMjQ0NjIiLCJ1dGkiOiI5cU92cGt5TDVVcWpjRnlrdGpWSkFBIiwidmVyIjoiMi4wIn0.F2oGM-zdqablnog4Xz64moSv5U-wT6xh1m5aNh5Jio1Oj7LPdSBCYWXx7wb0FQEUpjTvwYBaJaKSG9OECX7wNt0uQSRt78CEbGIw2hgQyYmgtlFbdOtYBlTs40YDLJaBmEdWwzurYDyt9ipCt7yCNDsg6mB5-pm0oIiqZC2mWywUriW_RMP5IL51RLWw7hoRXmWSKTp5arfqUJjSeFOgo9IN7qZBxBL08zBlYCXc81KCaFz0YepsbYzr3of70h3ZRwn1sJFvBMJJU3yhZ5Fj2m9Q3vzOpZu1AB0nA3QNmwIW-VhI4Hr9q7f14-4Mmtby4VBuTtbjQGfxGR0N7R7f8Q"",""local_account_id"":""ae821e4d-f408-451a-af82-882691148603"",""scopes"":""User.Read openid offline_access profile"",""success"":true,""tenant_id"":""49f548d0-12b7-4169-a390-bb5304d24462"",""tenant_profile_cache_records"":[{""mAccessToken"":{""access_token_type"":""Bearer"",""authority"":""https://login.microsoftonline.com/common"",""expires_on"":""1591270486"",""extended_expires_on"":""1591270486"",""realm"":""49f548d0-12b7-4169-a390-bb5304d24462"",""target"":""User.Read openid offline_access profile"",""cached_at"":""1591266887"",""client_id"":""4a1aa1d5-c567-49d0-ad0b-cd957a47f842"",""credential_type"":""AccessToken"",""environment"":""login.windows.net"",""home_account_id"":""ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462"",""secret"":""eyJ0eXAiOiJKV1QiLCJub25jZSI6IkhKQURvVFQ4a0ZRS1VMSWI5OGhiNU00V1NJMVh6eFJDRWZpRGlnbk5GWGciLCJhbGciOiJSUzI1NiIsIng1dCI6IlNzWnNCTmhaY0YzUTlTNHRycFFCVEJ5TlJSSSIsImtpZCI6IlNzWnNCTmhaY0YzUTlTNHRycFFCVEJ5TlJSSSJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC80OWY1NDhkMC0xMmI3LTQxNjktYTM5MC1iYjUzMDRkMjQ0NjIvIiwiaWF0IjoxNTkxMjY2NTg3LCJuYmYiOjE1OTEyNjY1ODcsImV4cCI6MTU5MTI3MDQ4NywiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IkFTUUEyLzhQQUFBQWt4VStBb0JzcDhJM3dCbmxKVC9aVjdrbE1KZktrRVVxUzdBcjg5R283ajQ9IiwiYW1yIjpbInB3ZCIsInJzYSJdLCJhcHBfZGlzcGxheW5hbWUiOiJQdWJsaWNDbGllbnRTYW1wbGUgKERPIE5PVCBVU0UgSU4gUFJPRFVDVElPTikiLCJhcHBpZCI6IjRhMWFhMWQ1LWM1NjctNDlkMC1hZDBiLWNkOTU3YTQ3Zjg0MiIsImFwcGlkYWNyIjoiMCIsImRldmljZWlkIjoiZDE3MTdkYTUtZjNkNi00NGE5LWFiNmYtZDYwYTEzNjQ5YmIwIiwiZmFtaWx5X25hbWUiOiJLYW5nIElJIiwiZ2l2ZW5fbmFtZSI6IkxpdSIsImlwYWRkciI6IjgxLjk2Ljg0LjE4OCIsIm5hbWUiOiJMaXUgS2FuZyIsIm9pZCI6ImFlODIxZTRkLWY0MDgtNDUxYS1hZjgyLTg4MjY5MTE0ODYwMyIsInBsYXRmIjoiMSIsInB1aWQiOiIxMDAzM0ZGRkFDMEYzMzk5IiwicHdkX2V4cCI6IjI5NTI3NjYiLCJwd2RfdXJsIjoiaHR0cHM6Ly9wb3J0YWwubWljcm9zb2Z0b25saW5lLmNvbS9DaGFuZ2VQYXNzd29yZC5hc3B4IiwicmgiOiIwLkFTa0EwRWoxU2JjU2FVR2prTHRUQk5KRVl0V2hHa3BueGRCSnJRdk5sWHBILUVJcEFBOC4iLCJzY3AiOiJvcGVuaWQgcHJvZmlsZSBVc2VyLlJlYWQgZW1haWwiLCJzdWIiOiJtekxqcmROaVlocnRldVhUd3NkdUFtdWwtbE9JRjBxX2RlMkNraHNvdU1nIiwidGVuYW50X3JlZ2lvbl9zY29wZSI6Ik5BIiwidGlkIjoiNDlmNTQ4ZDAtMTJiNy00MTY5LWEzOTAtYmI1MzA0ZDI0NDYyIiwidW5pcXVlX25hbWUiOiJsaXUua2FuZ0Bib2dhdnJpbExURC5vbm1pY3Jvc29mdC5jb20iLCJ1cG4iOiJsaXUua2FuZ0Bib2dhdnJpbExURC5vbm1pY3Jvc29mdC5jb20iLCJ1dGkiOiI5cU92cGt5TDVVcWpjRnlrdGpWSkFBIiwidmVyIjoiMS4wIiwid2lkcyI6WyI2MmU5MDM5NC02OWY1LTQyMzctOTE5MC0wMTIxNzcxNDVlMTAiXSwieG1zX3N0Ijp7InN1YiI6IkRfaGFOUXlsVzgzQTV3MlV0STk1clg3NDhtZHhLTlk3MkJHZ2Y4M21JN1kifSwieG1zX3RjZHQiOjE1Mjg4ODcwNTl9.rRP8sYzhwVHQ7gT1Ft5TU2fJi1S7_jVB5mAkHjZLiSQt6xG441c5Ey_0xTFvciOaheTw410ssSWXmGaaI1doRWOvt38_UJfaOIsMUM-msLznPT8Dzi5wGgvcTKKt_BF41wutqQavkVXwwmoUFPXDDLQwGc8N7WGLTn3mR7dDUP_wgFdPXx4wTbW1S3fPYmKJuNnc8OCk3fYMAL4TlJLWEklVswWjHCPmRAyVjzknQCof6Sz9to775pt1UrXxIZMUd5rsKATSoqqBnBzWt2kiFd_Atz5y0s2k21nvDywVsF0bhVE4YC7pHoVNHH5eEwh1Dbj5MOYRpA2VPdWXB6fpHg""},""mAccount"":{""authority_type"":""MSSTS"",""client_info"":""eyJ1aWQiOiJhZTgyMWU0ZC1mNDA4LTQ1MWEtYWY4Mi04ODI2OTExNDg2MDMiLCJ1dGlkIjoiNDlmNTQ4ZDAtMTJiNy00MTY5LWEzOTAtYmI1MzA0ZDI0NDYyIn0"",""environment"":""login.windows.net"",""home_account_id"":""ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462"",""local_account_id"":""ae821e4d-f408-451a-af82-882691148603"",""name"":""Liu Kang"",""realm"":""49f548d0-12b7-4169-a390-bb5304d24462"",""username"":""liu.kang@bogavrilLTD.onmicrosoft.com""},""mIdToken"":{""authority"":""https://login.microsoftonline.com/common"",""realm"":""49f548d0-12b7-4169-a390-bb5304d24462"",""client_id"":""4a1aa1d5-c567-49d0-ad0b-cd957a47f842"",""credential_type"":""IdToken"",""environment"":""login.windows.net"",""home_account_id"":""ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462"",""secret"":""eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IlNzWnNCTmhaY0YzUTlTNHRycFFCVEJ5TlJSSSJ9.eyJhdWQiOiI0YTFhYTFkNS1jNTY3LTQ5ZDAtYWQwYi1jZDk1N2E0N2Y4NDIiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNDlmNTQ4ZDAtMTJiNy00MTY5LWEzOTAtYmI1MzA0ZDI0NDYyL3YyLjAiLCJpYXQiOjE1OTEyNjY1ODcsIm5iZiI6MTU5MTI2NjU4NywiZXhwIjoxNTkxMjcwNDg3LCJuYW1lIjoiTGl1IEthbmciLCJvaWQiOiJhZTgyMWU0ZC1mNDA4LTQ1MWEtYWY4Mi04ODI2OTExNDg2MDMiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJsaXUua2FuZ0Bib2dhdnJpbExURC5vbm1pY3Jvc29mdC5jb20iLCJyaCI6IjAuQVNrQTBFajFTYmNTYVVHamtMdFRCTkpFWXRXaEdrcG54ZEJKclF2TmxYcEgtRUlwQUE4LiIsInN1YiI6IkRfaGFOUXlsVzgzQTV3MlV0STk1clg3NDhtZHhLTlk3MkJHZ2Y4M21JN1kiLCJ0aWQiOiI0OWY1NDhkMC0xMmI3LTQxNjktYTM5MC1iYjUzMDRkMjQ0NjIiLCJ1dGkiOiI5cU92cGt5TDVVcWpjRnlrdGpWSkFBIiwidmVyIjoiMi4wIn0.F2oGM-zdqablnog4Xz64moSv5U-wT6xh1m5aNh5Jio1Oj7LPdSBCYWXx7wb0FQEUpjTvwYBaJaKSG9OECX7wNt0uQSRt78CEbGIw2hgQyYmgtlFbdOtYBlTs40YDLJaBmEdWwzurYDyt9ipCt7yCNDsg6mB5-pm0oIiqZC2mWywUriW_RMP5IL51RLWw7hoRXmWSKTp5arfqUJjSeFOgo9IN7qZBxBL08zBlYCXc81KCaFz0YepsbYzr3of70h3ZRwn1sJFvBMJJU3yhZ5Fj2m9Q3vzOpZu1AB0nA3QNmwIW-VhI4Hr9q7f14-4Mmtby4VBuTtbjQGfxGR0N7R7f8Q""}}],""token_type"":""Bearer"",""username"":""liu.kang@bogavrilLTD.onmicrosoft.com""}"

            string unixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(40));

            string androidBrokerResponse = @"
{
      ""access_token"":""secretAt"",
      ""authority"":""https://login.microsoftonline.com/common"",
      ""cached_at"":1591193165,
      ""client_id"":""4a1aa1d5-c567-49d0-ad0b-cd957a47f842"",
      ""client_info"":""clientInfo"",
      ""environment"":""login.windows.net"",
      ""expires_on"":1591196764,
      ""ext_expires_on"":1591196764,
      ""home_account_id"":""ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462"",
      ""http_response_code"":0,
      ""id_token"":""idT"",
      ""local_account_id"":""ae821e4d-f408-451a-af82-882691148603"",
      ""scopes"":""User.Read openid offline_access profile"",
      ""success"":true,
      ""tenant_id"":""49f548d0-12b7-4169-a390-bb5304d24462"",     
      ""token_type"":""Bearer"",
      ""username"":""some_user@contoso.com""
   }";
            androidBrokerResponse.Replace("1591196764", unixTimestamp); 
            string correlationId = Guid.NewGuid().ToString();
            // Act
            var msalTokenResponse = MsalTokenResponse.CreateFromAndroidBrokerResponse(androidBrokerResponse, correlationId);

            // Assert
            Assert.AreEqual("secretAt", msalTokenResponse.AccessToken);
            Assert.AreEqual(correlationId, msalTokenResponse.CorrelationId);
            Assert.AreEqual("https://login.microsoftonline.com/common", msalTokenResponse.Authority);
            Assert.AreEqual("clientInfo", msalTokenResponse.ClientInfo);
            Assert.AreEqual("idT", msalTokenResponse.IdToken);
            Assert.AreEqual("User.Read openid offline_access profile", msalTokenResponse.Scope);
            Assert.AreEqual("Bearer", msalTokenResponse.TokenType);
            Assert.IsTrue(msalTokenResponse.AccessTokenExpiresOn <= DateTimeOffset.Now + TimeSpan.FromMinutes(40));

            Assert.IsNull(msalTokenResponse.RefreshToken);

        }
    }
}
