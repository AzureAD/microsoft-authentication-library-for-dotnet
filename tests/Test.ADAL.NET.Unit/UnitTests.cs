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
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Owin;

using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("valid_cert.pfx")]
    [DeploymentItem("valid_cert2.pfx")]
    [DeploymentItem("Microsoft.Owin.Host.HttpListener.dll")]
    public class UnitTests
    {
        private const string ComplexString = "asdfk+j0a-=skjwe43;1l234 1#$!$#%345903485qrq@#$!@#$!(rekr341!#$%Ekfaآزمايشsdsdfsddfdgsfgjsglk==CVADS";
        private const string ComplexString2 = @"a\u0304\u0308"" = ""ā̈";

        [TestMethod]
        [Description("Positive Test for UrlEncoding")]
        [TestCategory("AdalDotNetUnit")]
        public void UrlEncodingTest()
        {
            TestUrlEncoding(null);
            TestUrlEncoding(string.Empty);
            TestUrlEncoding("   ");
            TestUrlEncoding(ComplexString);
            TestUrlEncoding(ComplexString2);
        }

        [TestMethod]
        [Description("Test for RequestParameters class")]
        [TestCategory("AdalDotNetUnit")]
        public void RequestParametersTest()
        {
            const string ClientId = "client_id";
            const string AdditionalParameter = "additional_parameter";
            const string AdditionalParameter2 = "additional_parameter2";
            string expectedString = string.Format("client_id=client_id&{0}={1}&{2}={3}", AdditionalParameter, EncodingHelper.UrlEncode(ComplexString), AdditionalParameter2, EncodingHelper.UrlEncode(ComplexString2));

            var param = new DictionaryRequestParameters(null, new ClientKey(ClientId));
            param[AdditionalParameter] = ComplexString;
            param[AdditionalParameter2] = ComplexString2;
            Verify.AreEqual(expectedString, param.ToString());

            param = new DictionaryRequestParameters(null, new ClientKey(ClientId));
            param[AdditionalParameter] = ComplexString;
            param[AdditionalParameter2] = ComplexString2;
            Verify.AreEqual(expectedString, param.ToString());

            param = new DictionaryRequestParameters(null, new ClientKey(ClientId));
            param[AdditionalParameter] = ComplexString;
            param[AdditionalParameter2] = ComplexString2;
            Verify.AreEqual(expectedString, param.ToString());

            var stringParam = new StringRequestParameters(new StringBuilder(expectedString));
            Verify.AreEqual(expectedString, stringParam.ToString());
        }

        [TestMethod]
        [Description("Test for authority type detection")]
        [TestCategory("AdalDotNetUnit")]
        public void AuthorityTypeDetectionTest()
        {
            Verify.AreEqual(AuthorityType.AAD, Authenticator.DetectAuthorityType("https://login.windows.net/tenant/dummy/"));
            Verify.AreEqual(AuthorityType.AAD, Authenticator.DetectAuthorityType("https://accounts-int.somethingelse.w/dummy/"));
            Verify.AreEqual(AuthorityType.ADFS, Authenticator.DetectAuthorityType("https://abc.com/adfs/dummy/"));
        }


        [TestMethod]
        [Description("Test for AuthenticationParameters.CreateFromResponseAuthenticateHeader")]
        [TestCategory("AdalDotNetUnit")]
        public void AuthenticationParametersTest()
        {
            RunAuthenticationParametersPositive("Bearer authorization_uri=abc, resource_id=de", "abc", "de");
            RunAuthenticationParametersPositive("Bearer authorization_uri=\"https://login.windows.net/tenant_name/oauth2/authorize\", resource_id=de", "https://login.windows.net/tenant_name/oauth2/authorize", "de");
            RunAuthenticationParametersPositive("Bearer authorization_uri=\"abc\", resource_id=\"de\"", "abc", "de");
            RunAuthenticationParametersPositive(" Bearer authorization_uri=abc, resource_id=de", "abc", "de");
            RunAuthenticationParametersPositive("bearer Authorization_uri=abc, resource_ID=de", "abc", "de");
            RunAuthenticationParametersPositive("Bearer authorization_uri=abc, extra=\"abcd\" , resource_id=\"de=x,y\",extra2=\"fgh+s\"", "abc", "de=x,y");
            RunAuthenticationParametersPositive("Bearer authorization_uri=abc, resource_idx=de", "abc", null);
            RunAuthenticationParametersPositive("Bearer authorization_urix=abc, resource_id=de", null, "de");
            RunAuthenticationParametersPositive("Bearer\tauthorization_uri=abc, resource_id=de", "abc", "de");
            RunAuthenticationParametersPositive("Bearer x", null, null);

            RunAuthenticationParametersNegative(null);
            RunAuthenticationParametersNegative(string.Empty);
            RunAuthenticationParametersNegative("abc");
            RunAuthenticationParametersNegative("Bearer");
            RunAuthenticationParametersNegative("Bearer ");
            RunAuthenticationParametersNegative("BearerX");
            RunAuthenticationParametersNegative("BearerX authorization_uri=\"abc\", resource_id=\"de\"");
            RunAuthenticationParametersNegative("Bearer authorization_uri=\"abc\"=\"de\"");
            RunAuthenticationParametersNegative("Bearer authorization_uri=abc=de");
        }

        [TestMethod]
        [Description("Test for ParseKeyValueList method in EncodingHelper")]
        [TestCategory("AdalDotNetUnit")]
        public void ParseKeyValueListTest()
        {
            RunParseKeyValueList(null, 0);
            RunParseKeyValueList(string.Empty, 0);
            RunParseKeyValueList("abc=", 0);
            RunParseKeyValueList("=x", 0);
            RunParseKeyValueList("abc=x", 1, new[] { "abc" }, new[] { "x" });
            RunParseKeyValueList("abc=x=y", 0);
            RunParseKeyValueList("abc=\"x=y\"", 1, new[] { "abc" }, new[] { "x=y" });
            RunParseKeyValueList("abc=x,de=yz", 2, new[] { "abc", "de" }, new[] { "x", "yz" });
            RunParseKeyValueList("abc=\"x\",de=\"yz\"", 2, new[] { "abc", "de" }, new[] { "x", "yz" });
            RunParseKeyValueList("abc=\"x=u\",de=\"yz,t\"", 2, new[] { "abc", "de" }, new[] { "x=u", "yz,t" });
            RunParseKeyValueList(" abc  =   \" x=u\" ,   de= \"yz,t  \" ", 2, new[] { "abc", "de" }, new[] { "x=u", "yz,t" });
            RunParseKeyValueList(" abc  =\t   \" x=u\" ,   de= \"yz,t  \"\t ", 2, new[] { "abc", "de" }, new[] { "x=u", "yz,t" });
            RunParseKeyValueList(" abc  =\t   \" x=u\" ,   de= \"yz,t  \t\"\t ", 2, new[] { "abc", "de" }, new[] { "x=u", "yz,t" });
            RunParseKeyValueList("abc=x,abc=yz", 1, new[] { "abc" }, new[] { "yz" });

            RunParseKeyValueList("abc=\"x=u\",de=\"yz,t\"", 2, new[] { "abc", "de" }, new[] { "x=u", "yz,t" }, true);
            RunParseKeyValueList("abc=\"x%3Du\",de=\"yz%2Ct\"", 2, new[] { "abc", "de" }, new[] { "x=u", "yz,t" }, true);
            RunParseKeyValueList("abc=\"x%3Du\",de=\"yz%2Ct\"", 2, new[] { "abc", "de" }, new[] { "x%3Du", "yz%2Ct" });
        }

        [TestMethod]
        [Description("Test for SplitWithQuotes method in EncodingHelper")]
        [TestCategory("AdalDotNetUnit")]
        public void SplitWithQuotesTest()
        {
            RunSplitWithQuotes(null, 0);
            RunSplitWithQuotes(string.Empty, 0);
            RunSplitWithQuotes(",", 0);
            RunSplitWithQuotes(",abc", 1, "abc");
            RunSplitWithQuotes("abc,", 1, "abc");
            RunSplitWithQuotes("abc", 1, "abc");
            RunSplitWithQuotes("abc,", 1, "abc");
            RunSplitWithQuotes(@"""abc""", 1, @"""abc""");
            RunSplitWithQuotes(@"""abc,de""", 1, @"""abc,de""");
            RunSplitWithQuotes(@""" abc        ,   de  """, 1, @""" abc        ,   de  """);
            RunSplitWithQuotes("abc, def", 2, "abc", " def");
            RunSplitWithQuotes("abc=x,def=yz", 2, "abc=x", "def=yz");
            RunSplitWithQuotes(@"""abc"", ""def""", 2, @"""abc""", @" ""def""");
            RunSplitWithQuotes(@"""abc"", ""def,ef""", 2, @"""abc""", @" ""def,ef""");
        }

        [TestMethod]
        [Description("Test for CreateSha256Hash method in PlatformSpecificHelper")]
        [TestCategory("AdalDotNetUnit")]
        public void CreateSha256HashTest()
        {
            CommonUnitTests.CreateSha256HashTest();
        }

        [TestMethod]
        [Description("Test for ADAL Id")]
        [TestCategory("AdalDotNetUnit")]
        public void AdalIdTest()
        {
            CommonUnitTests.AdalIdTest();
        }
        
        [TestMethod]
        [Description("Test for Id Token Parsing")]
        [TestCategory("AdalDotNetUnit")]
        public void IdTokenParsingPasswordClaimsTest()
        {
            TokenResponse tr = CreateTokenResponse();
            tr.IdTokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiI5MDgzY2NiOC04YTQ2LTQzZTctODQzOS0xZDY5NmRmOTg0YWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8zMGJhYTY2Ni04ZGY4LTQ4ZTctOTdlNi03N2NmZDA5OTU5NjMvIiwiaWF0IjoxNDAwNTQxMzk1LCJuYmYiOjE0MDA1NDEzOTUsImV4cCI6MTQwMDU0NTU5NSwidmVyIjoiMS4wIiwidGlkIjoiMzBiYWE2NjYtOGRmOC00OGU3LTk3ZTYtNzdjZmQwOTk1OTYzIiwib2lkIjoiNGY4NTk5ODktYTJmZi00MTFlLTkwNDgtYzMyMjI0N2FjNjJjIiwidXBuIjoiYWRtaW5AYWFsdGVzdHMub25taWNyb3NvZnQuY29tIiwidW5pcXVlX25hbWUiOiJhZG1pbkBhYWx0ZXN0cy5vbm1pY3Jvc29mdC5jb20iLCJzdWIiOiJCczVxVG4xQ3YtNC10VXIxTGxBb3pOS1NRd0Fjbm4ydHcyQjlmelduNlpJIiwiZmFtaWx5X25hbWUiOiJBZG1pbiIsImdpdmVuX25hbWUiOiJBREFMVGVzdHMiLCJwd2RfZXhwIjoiMzYwMDAiLCJwd2RfdXJsIjoiaHR0cHM6Ly9jaGFuZ2VfcHdkLmNvbSJ9.";
            AuthenticationResultEx result = tr.GetResult();
            Verify.AreEqual(result.Result.User.PasswordChangeUrl, "https://change_pwd.com");
            Verify.IsNotNull(result.Result.User.PasswordExpiresOn);
        }

        [TestMethod]
        [Description("Test for Id Token Parsing")]
        [TestCategory("AdalDotNetUnit")]
        public void IdTokenParsingNoPasswordClaimsTest()
        {
            TokenResponse tr = CreateTokenResponse();
            tr.IdTokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiI5MDgzY2NiOC04YTQ2LTQzZTctODQzOS0xZDY5NmRmOTg0YWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8zMGJhYTY2Ni04ZGY4LTQ4ZTctOTdlNi03N2NmZDA5OTU5NjMvIiwiaWF0IjoxNDAwNTQxMzk1LCJuYmYiOjE0MDA1NDEzOTUsImV4cCI6MTQwMDU0NTU5NSwidmVyIjoiMS4wIiwidGlkIjoiMzBiYWE2NjYtOGRmOC00OGU3LTk3ZTYtNzdjZmQwOTk1OTYzIiwib2lkIjoiNGY4NTk5ODktYTJmZi00MTFlLTkwNDgtYzMyMjI0N2FjNjJjIiwidXBuIjoiYWRtaW5AYWFsdGVzdHMub25taWNyb3NvZnQuY29tIiwidW5pcXVlX25hbWUiOiJhZG1pbkBhYWx0ZXN0cy5vbm1pY3Jvc29mdC5jb20iLCJzdWIiOiJCczVxVG4xQ3YtNC10VXIxTGxBb3pOS1NRd0Fjbm4ydHcyQjlmelduNlpJIiwiZmFtaWx5X25hbWUiOiJBZG1pbiIsImdpdmVuX25hbWUiOiJBREFMVGVzdHMifQ.";
            AuthenticationResultEx result = tr.GetResult();
            Verify.IsNull(result.Result.User.PasswordChangeUrl);
            Verify.IsNull(result.Result.User.PasswordExpiresOn);
        }

        private static TokenResponse CreateTokenResponse()
        {
            return new TokenResponse
                               {
                                   AccessToken = "access_token",
                                   RefreshToken = "refresh_token",
                                   CorrelationId = Guid.NewGuid().ToString(),
                                   Resource = "my-resource",
                                   TokenType = "Bearer",
                                   ExpiresIn = 3899
                               };
        }

        [TestMethod]
        [TestCategory("AdalDotNetUnit")]
        [Description("Test to verify CryptographyHelper.SignWithCertificate")]
        public void SignWithCertificateTest()
        {
            const string Message = "This is a test message";
            string[] certs = { "valid_cert.pfx", "valid_cert2.pfx" };
            for (int i = 0; i < 2; i++)
            {
                X509Certificate2 x509Certificate = new X509Certificate2(certs[i], "password", X509KeyStorageFlags.Exportable);
                byte[] rawData = x509Certificate.Export(X509ContentType.Pkcs12, "password");

                ICryptographyHelper cryptoHelper = new CryptographyHelper();
                byte[] signature = cryptoHelper.SignWithCertificate(Message, rawData, "password");
                Verify.IsNotNull(signature);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                signature = cryptoHelper.SignWithCertificate(Message, rawData, "password");
                Verify.IsNotNull(signature);
            }
        }

        [TestMethod]
        [TestCategory("AdalDotNetUnit")]
        public async Task TimeoutTest()
        {
            const string TestServiceUrl = "http://localhost:8080";
            using (WebApp.Start<TestService>(TestServiceUrl))
            {
                HttpClientWrapper webClient = new HttpClientWrapper(TestServiceUrl + "?delay=0&response_code=200", null) { TimeoutInMilliSeconds = 10000 };
                await webClient.GetResponseAsync();

                webClient = new HttpClientWrapper(TestServiceUrl + "?delay=0&response_code=200", null) { TimeoutInMilliSeconds = 10000 };
                await webClient.GetResponseAsync();

                try
                {
                    webClient = new HttpClientWrapper(TestServiceUrl + "?delay=0&response_code=400", null) { TimeoutInMilliSeconds = 10000 };
                    await webClient.GetResponseAsync();
                }
                catch (HttpRequestWrapperException ex)
                {
                    Verify.AreEqual(ex.WebResponse.StatusCode, HttpStatusCode.BadRequest);
                }


                try
                {
                    webClient = new HttpClientWrapper(TestServiceUrl + "?delay=10000&response_code=200", null) { TimeoutInMilliSeconds = 500 };
                    await webClient.GetResponseAsync();
                }
                catch (HttpRequestWrapperException ex)
                {
                    Verify.IsTrue(ex.InnerException is TaskCanceledException);
                    var serviceException = new MsalServiceException(MsalError.Unknown, ex);
                    Verify.AreEqual(serviceException.StatusCode, (int)HttpStatusCode.RequestTimeout);
                }
            }
        }
        
        private static void RunAuthenticationParametersPositive(string authenticateHeader, string expectedAuthority, string excepectedResource)
        {
            AuthenticationParameters parameters = AuthenticationParameters.CreateFromResponseAuthenticateHeader(authenticateHeader);
            Verify.AreEqual(expectedAuthority, parameters.Authority);
            Verify.AreEqual(excepectedResource, parameters.Resource);            
        }

        private static void RunAuthenticationParametersNegative(string authenticateHeader)
        {
            try
            {
                AuthenticationParameters.CreateFromResponseAuthenticateHeader(authenticateHeader);
            }
            catch (ArgumentException ex)
            {
                Verify.AreEqual("authenticateHeader", ex.ParamName);
                Verify.IsTrue(string.IsNullOrWhiteSpace(authenticateHeader) || ex.Message.Contains("header format"));
            }
        }

        private static void RunParseKeyValueList(string input, int expectedCount, string[] keys = null, string[] values = null, bool urlDecode = false)
        {
            Dictionary<string, string> result = EncodingHelper.ParseKeyValueList(input, ',', urlDecode, null);
            Verify.AreEqual(expectedCount, result.Count);
            if (keys != null && values != null)
            {
                for (int i = 0; i < expectedCount; i++)
                {
                    Verify.AreEqual(result[keys[i]], values[i]);
                }
            }
        }

        private static void RunSplitWithQuotes(string input, int expectedCount, string first = null, string second = null)
        {
            List<string> items = EncodingHelper.SplitWithQuotes(input, ',');
            Verify.AreEqual(expectedCount, items.Count);
            if (first != null)
            {
                Verify.AreEqual(first, items[0]);
            }

            if (second != null)
            {
                Verify.AreEqual(second, items[1]);
            }
        }

        private static void TestUrlEncoding(string str)
        {
            string encodedStr = EncodingHelper.UrlEncode(str);

            char[] encodedChars = EncodingHelper.UrlEncode((str == null) ? null : str.ToCharArray());
            string encodedStr2 = (encodedChars == null) ? null : new string(encodedChars);

            Verify.AreEqual(encodedStr, encodedStr2);            
        }

        internal class TestService
        {
            public void Configuration(IAppBuilder app)
            {
                app.Run(ctx =>
                {
                    int delay = int.Parse(ctx.Request.Query["delay"]);
                    if (delay > 0)
                    {
                        Thread.Sleep(delay);
                    }

                    var response = ctx.Response;
                    response.StatusCode = int.Parse(ctx.Request.Query["response_code"]);
                    return response.WriteAsync("dummy");
                });
            }
        }
    }
}
