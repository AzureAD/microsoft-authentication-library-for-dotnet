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
using System.Security;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
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
        [Description("Positive Test for SecureString conversions")]
        [TestCategory("AdalDotNetUnit")]
        public void SecureStringTest()
        {
            TestSecureStringToCharArray(string.Empty);
            TestSecureStringToCharArray("   ");
            TestSecureStringToCharArray(ComplexString);
            TestSecureStringToCharArray(ComplexString2);
        }

        [TestMethod]
        [Description("Test for RequestParameters class")]
        [TestCategory("AdalDotNetUnit")]
        public void RequestParametersTest()
        {
            const string ClientId = "clientId";
            const string Resource = "resource";
            string expectedString = string.Format("{0}={1}&{2}={3}", ClientId, EncodingHelper.UrlEncode(ComplexString), Resource, EncodingHelper.UrlEncode(ComplexString2));

            RequestParameters param = new RequestParameters();
            param[ClientId] = ComplexString;
            param[Resource] = ComplexString2;
            Verify.AreEqual(expectedString, param.ToString());

            param = new RequestParameters();
            param[ClientId] = ComplexString;
            param.AddSecureParameter(Resource, StringToSecureString(ComplexString2));
            Verify.AreEqual(expectedString, param.ToString());

            param = new RequestParameters();
            param.AddSecureParameter(ClientId, StringToSecureString(ComplexString));
            param.AddSecureParameter(Resource, StringToSecureString(ComplexString2));
            Verify.AreEqual(expectedString, param.ToString());

            param = new RequestParameters(new StringBuilder(expectedString));
            Verify.AreEqual(expectedString, param.ToString());
        }

        [TestMethod]
        [Description("Test for RegexUtilities helper class")]
        [TestCategory("AdalDotNetUnit")]
        public void RegexUtilitiesTest()
        {
            Verify.IsFalse(RegexUtilities.IsValidEmail("@majjf.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("A@b@c@example.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("Abc.example.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("j..s@proseware.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("j.@server1.proseware.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("js*@proseware.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("js@proseware..com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma...ma@jjf.co"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma.@jjf.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma@@jjf.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma@jjf."));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma@jjf..com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma@jjf.c"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma_@jjf"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma_@jjf."));
            Verify.IsFalse(RegexUtilities.IsValidEmail("ma_@jjf.com"));
            Verify.IsFalse(RegexUtilities.IsValidEmail("-------"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("12@hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("d.j@server1.proseware.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("david.jones@proseware.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("j.s@server1.proseware.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("j@proseware.com9"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("j_9@[129.126.118.1]"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("jones@ms1.proseware.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("js@proseware.com9"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("m.a@hostname.co"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("m_a1a@hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma.h.saraf.onemore@hostname.com.edu"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma@hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma@hostname.comcom"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("MA@hostname.coMCom"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma12@hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma-a.aa@hostname.com.edu"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma-a@hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma-a@hostname.com.edu"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma-a@1hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma.a@1hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("ma@1hostname.com"));
            Verify.IsTrue(RegexUtilities.IsValidEmail("js#internal@proseware.com"));
        }

        [TestMethod]
        [Description("Test for authority type detection")]
        [TestCategory("AdalDotNetUnit")]
        public void AuthorityTypeDetectionTest()
        {
            Verify.AreEqual(AuthorityType.AAD, AuthenticationMetadata.DetectAuthorityType(AuthenticationMetadata.CanonicalizeUri("https://login.windows.net/tenant/dummy")));
            Verify.AreEqual(AuthorityType.AAD, AuthenticationMetadata.DetectAuthorityType(AuthenticationMetadata.CanonicalizeUri("https://accounts-int.somethingelse.w/dummy")));
            Verify.AreEqual(AuthorityType.ADFS, AuthenticationMetadata.DetectAuthorityType(AuthenticationMetadata.CanonicalizeUri("https://abc.com/adfs/dummy")));
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

        private void TestUrlEncoding(string str)
        {
            string encodedStr = EncodingHelper.UrlEncode(str);

            char[] encodedChars = EncodingHelper.UrlEncode((str == null) ? null : str.ToCharArray());
            string encodedStr2 = (encodedChars == null) ? null : new string(encodedChars);

            Verify.AreEqual(encodedStr, encodedStr2);            
        }

        private void TestSecureStringToCharArray(string str)
        {
            var secureStr = StringToSecureString(str);

            char[] secureChars = secureStr.ToCharArray();
            var secureStringRestored = new string(secureChars);

            Verify.AreEqual(str, secureStringRestored);            
        }

        private SecureString StringToSecureString(string str)
        {
            var secureStr = new SecureString();

            foreach (char ch in str)
                secureStr.AppendChar(ch);

            secureStr.MakeReadOnly();

            return secureStr;
        }
    }
}
