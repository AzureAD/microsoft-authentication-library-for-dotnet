//----------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public void UrlEncodingTest()
        {
            TestUrlEncoding(null);
            TestUrlEncoding(string.Empty);
            TestUrlEncoding("   ");
            TestUrlEncoding(ComplexString);
            TestUrlEncoding(ComplexString2);
            TestUrlEncoding("@");
        }

        [TestMethod]
        [Description("Test for RequestParameters class")]
        public void RequestParametersTest()
        {
            const string ClientId = "client_id";
            const string AdditionalParameter = "additional_parameter";
            const string AdditionalParameter2 = "additional_parameter2";
            string expectedString = string.Format(CultureInfo.CurrentCulture, "client_id=client_id&{0}={1}&{2}={3}", AdditionalParameter, EncodingHelper.UrlEncode(ComplexString), AdditionalParameter2, EncodingHelper.UrlEncode(ComplexString2));

            var param = new DictionaryRequestParameters(null, new ClientKey(ClientId));
            param[AdditionalParameter] = ComplexString;
            param[AdditionalParameter2] = ComplexString2;
            Assert.AreEqual(expectedString, param.ToString());

            param = new DictionaryRequestParameters(null, new ClientKey(ClientId));
            param[AdditionalParameter] = ComplexString;
            param[AdditionalParameter2] = ComplexString2;
            Assert.AreEqual(expectedString, param.ToString());

            param = new DictionaryRequestParameters(null, new ClientKey(ClientId));
            param[AdditionalParameter] = ComplexString;
            param[AdditionalParameter2] = ComplexString2;
            Assert.AreEqual(expectedString, param.ToString());

            var stringParam = new StringRequestParameters(new StringBuilder(expectedString));
            Assert.AreEqual(expectedString, stringParam.ToString());
        }

        [TestMethod]
        [Description("Test for authority type detection")]
        public void AuthorityTypeDetectionTest()
        {
            Assert.AreEqual(AuthorityType.AAD, Authenticator.DetectAuthorityType("https://login.windows.net/tenant/dummy/"));
            Assert.AreEqual(AuthorityType.AAD, Authenticator.DetectAuthorityType("https://accounts-int.somethingelse.w/dummy/"));
            Assert.AreEqual(AuthorityType.ADFS, Authenticator.DetectAuthorityType("https://abc.com/adfs/dummy/"));
        }


        [TestMethod]
        [Description("Test for AuthenticationParameters.CreateFromResponseAuthenticateHeader")]
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
        public void CreateSha256HashTest()
        {
            CommonUnitTests.CreateSha256HashTest();
        }

        [TestMethod]
        [Description("Test for ADAL Id")]
        public void AdalIdTest()
        {
            CommonUnitTests.AdalIdTest();
        }
        
        [TestMethod]
        [Description("Test for Id Token Parsing")]
        public void IdTokenParsingPasswordClaimsTest()
        {
            TokenResponse tr = CreateTokenResponse();
            tr.IdTokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiI5MDgzY2NiOC04YTQ2LTQzZTctODQzOS0xZDY5NmRmOTg0YWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8zMGJhYTY2Ni04ZGY4LTQ4ZTctOTdlNi03N2NmZDA5OTU5NjMvIiwiaWF0IjoxNDAwNTQxMzk1LCJuYmYiOjE0MDA1NDEzOTUsImV4cCI6MTQwMDU0NTU5NSwidmVyIjoiMS4wIiwidGlkIjoiMzBiYWE2NjYtOGRmOC00OGU3LTk3ZTYtNzdjZmQwOTk1OTYzIiwib2lkIjoiNGY4NTk5ODktYTJmZi00MTFlLTkwNDgtYzMyMjI0N2FjNjJjIiwidXBuIjoiYWRtaW5AYWFsdGVzdHMub25taWNyb3NvZnQuY29tIiwidW5pcXVlX25hbWUiOiJhZG1pbkBhYWx0ZXN0cy5vbm1pY3Jvc29mdC5jb20iLCJzdWIiOiJCczVxVG4xQ3YtNC10VXIxTGxBb3pOS1NRd0Fjbm4ydHcyQjlmelduNlpJIiwiZmFtaWx5X25hbWUiOiJBZG1pbiIsImdpdmVuX25hbWUiOiJBREFMVGVzdHMiLCJwd2RfZXhwIjoiMzYwMDAiLCJwd2RfdXJsIjoiaHR0cHM6Ly9jaGFuZ2VfcHdkLmNvbSJ9.";
            AuthenticationResultEx result = tr.GetResult();
            Assert.AreEqual(result.Result.UserInfo.PasswordChangeUrl, "https://change_pwd.com");
            Assert.IsNotNull(result.Result.UserInfo.PasswordExpiresOn);
        }

        [TestMethod]
        [Description("Test for Id Token Parsing")]
        public void IdTokenParsingNoPasswordClaimsTest()
        {
            TokenResponse tr = CreateTokenResponse();
            tr.IdTokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiI5MDgzY2NiOC04YTQ2LTQzZTctODQzOS0xZDY5NmRmOTg0YWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8zMGJhYTY2Ni04ZGY4LTQ4ZTctOTdlNi03N2NmZDA5OTU5NjMvIiwiaWF0IjoxNDAwNTQxMzk1LCJuYmYiOjE0MDA1NDEzOTUsImV4cCI6MTQwMDU0NTU5NSwidmVyIjoiMS4wIiwidGlkIjoiMzBiYWE2NjYtOGRmOC00OGU3LTk3ZTYtNzdjZmQwOTk1OTYzIiwib2lkIjoiNGY4NTk5ODktYTJmZi00MTFlLTkwNDgtYzMyMjI0N2FjNjJjIiwidXBuIjoiYWRtaW5AYWFsdGVzdHMub25taWNyb3NvZnQuY29tIiwidW5pcXVlX25hbWUiOiJhZG1pbkBhYWx0ZXN0cy5vbm1pY3Jvc29mdC5jb20iLCJzdWIiOiJCczVxVG4xQ3YtNC10VXIxTGxBb3pOS1NRd0Fjbm4ydHcyQjlmelduNlpJIiwiZmFtaWx5X25hbWUiOiJBZG1pbiIsImdpdmVuX25hbWUiOiJBREFMVGVzdHMifQ.";
            AuthenticationResultEx result = tr.GetResult();
            Assert.IsNull(result.Result.UserInfo.PasswordChangeUrl);
            Assert.IsNull(result.Result.UserInfo.PasswordExpiresOn);
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
        [Description("Test to verify CryptographyHelper.SignWithCertificate")]
        public void SignWithCertificateTest()
        {
            const string Message = "This is a test message";
            string[] certs = { "valid_cert.pfx", "valid_cert2.pfx" };
            for (int i = 0; i < 2; i++)
            {
                X509Certificate2 x509Certificate = new X509Certificate2(certs[i], "password");
                ClientAssertionCertificate cac = new ClientAssertionCertificate("some_id", x509Certificate);
                byte[] signature = cac.Sign(Message);
                Assert.IsNotNull(signature);
                
                GC.WaitForPendingFinalizers();

                signature = cac.Sign(Message);
                Assert.IsNotNull(signature);
            }
        }
        
        private static void RunAuthenticationParametersPositive(string authenticateHeader, string expectedAuthority, string excepectedResource)
        {
            AuthenticationParameters parameters = AuthenticationParameters.CreateFromResponseAuthenticateHeader(authenticateHeader);
            Assert.AreEqual(expectedAuthority, parameters.Authority);
            Assert.AreEqual(excepectedResource, parameters.Resource);            
        }

        private static void RunAuthenticationParametersNegative(string authenticateHeader)
        {
            try
            {
                AuthenticationParameters.CreateFromResponseAuthenticateHeader(authenticateHeader);
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("authenticateHeader", ex.ParamName);
                Assert.IsTrue(string.IsNullOrWhiteSpace(authenticateHeader) || ex.Message.Contains("header format"));
            }
        }

        private static void RunParseKeyValueList(string input, int expectedCount, string[] keys = null, string[] values = null, bool urlDecode = false)
        {
            Dictionary<string, string> result = EncodingHelper.ParseKeyValueList(input, ',', urlDecode, null);
            Assert.AreEqual(expectedCount, result.Count);
            if (keys != null && values != null)
            {
                for (int i = 0; i < expectedCount; i++)
                {
                    Assert.AreEqual(result[keys[i]], values[i]);
                }
            }
        }

        private static void RunSplitWithQuotes(string input, int expectedCount, string first = null, string second = null)
        {
            List<string> items = EncodingHelper.SplitWithQuotes(input, ',');
            Assert.AreEqual(expectedCount, items.Count);
            if (first != null)
            {
                Assert.AreEqual(first, items[0]);
            }

            if (second != null)
            {
                Assert.AreEqual(second, items[1]);
            }
        }

        private static void TestUrlEncoding(string str)
        {
            string encodedStr = EncodingHelper.UrlEncode(str);

            char[] encodedChars = EncodingHelper.UrlEncode((str == null) ? null : str.ToCharArray());
            string encodedStr2 = (encodedChars == null) ? null : new string(encodedChars);

            Assert.AreEqual(encodedStr, encodedStr2);            
        }
    }
}
