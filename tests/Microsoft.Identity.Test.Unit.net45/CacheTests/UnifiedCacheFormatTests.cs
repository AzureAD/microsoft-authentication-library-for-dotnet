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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if !NET_CORE

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    [DeploymentItem(@"Resources\AADTestData.txt")]
    [DeploymentItem(@"Resources\MSATestData.txt")]
    [DeploymentItem(@"Resources\B2CNoTenantIdTestData.txt")]
    [DeploymentItem(@"Resources\B2CWithTenantIdTestData.txt")]
    public class UnifiedCacheFormatTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private void Init(MockHttpManager httpManager)
        {
            httpManager.AddMockHandler(
            MockHelpers.CreateInstanceDiscoveryMockHandler(
                MsalTestConstants.GetDiscoveryEndpoint(MsalTestConstants.AuthorityCommonTenant)));
        }

        private string _clientId;
        private string _requestAuthority;

        private string _tokenResponse;
        private string _idTokenResponse;

        private string _expectedAtCacheKey;
        private string _expectedAtCacheKeyIosService;
        private string _expectedAtCacheKeyIosAccount;
        private string _expectedAtCacheKeyIosGeneric;
        private string _expectedAtCacheValue;

        private string _expectedIdTokenCacheKey;
        private string _expectedIdTokenCacheKeyIosService;
        private string _expectedIdTokenCacheKeyIosAccount;
        private string _expectedIdTokenCacheKeyIosGeneric;
        private string _expectedIdTokenCacheValue;

        private string _expectedRtCacheKey;
        private string _expectedRtCacheKeyIosService;
        private string _expectedRtCacheKeyIosAccount;
        private string _expectedRtCacheKeyIosGeneric;

        private string _expectedAccountCacheKey;
        private string _expectedAccountCacheKeyIosService;
        private string _expectedAccountCacheKeyIosAccount;
        private string _expectedAccountCacheKeyIosGeneric;
        private string _expectedAccountCacheValue;
        private string _expectedRtCacheValue;

        private readonly RequestContext _requestContext = RequestContext.CreateForTest();

        private void IntitTestData(string fileName)
        {
            using (StreamReader r = new StreamReader(fileName))
            {
                string json = r.ReadToEnd();
                var configJson = JsonConvert.DeserializeObject<JObject>(json);

                _clientId = configJson.GetValue("client_id").ToString();
                _requestAuthority = configJson.GetValue("authority").ToString();

                _tokenResponse = configJson.GetValue("token_response").ToString();
                _idTokenResponse = configJson.GetValue("id_token_response").ToString();

                _expectedAtCacheKey = configJson.GetValue("at_cache_key").ToString();
                _expectedAtCacheKeyIosService = configJson.GetValue("at_cache_key_ios_service").ToString();
                _expectedAtCacheKeyIosAccount = configJson.GetValue("at_cache_key_ios_account").ToString();
                _expectedAtCacheKeyIosGeneric = configJson.GetValue("at_cache_key_ios_generic").ToString();
                _expectedAtCacheKey = configJson.GetValue("at_cache_key").ToString();

                _expectedAtCacheValue = configJson.GetValue("at_cache_value").ToString();

                _expectedIdTokenCacheKey = configJson.GetValue("id_token_cache_key").ToString();
                _expectedIdTokenCacheKeyIosService = configJson.GetValue("id_token_cache_key_ios_service").ToString();
                _expectedIdTokenCacheKeyIosAccount = configJson.GetValue("id_token_cache_key_ios_account").ToString();
                _expectedIdTokenCacheKeyIosGeneric = configJson.GetValue("id_token_cache_key_ios_generic").ToString();
                _expectedIdTokenCacheValue = configJson.GetValue("id_token_cache_value").ToString();

                _expectedRtCacheKey = configJson.GetValue("rt_cache_key").ToString();
                _expectedRtCacheKeyIosService = configJson.GetValue("rt_cache_key_ios_service").ToString();
                _expectedRtCacheKeyIosAccount = configJson.GetValue("rt_cache_key_ios_account").ToString();
                _expectedRtCacheKeyIosGeneric = configJson.GetValue("rt_cache_key_ios_generic").ToString();
                _expectedRtCacheValue = configJson.GetValue("rt_cache_value").ToString();

                _expectedAccountCacheKey = configJson.GetValue("account_cache_key").ToString();
                _expectedAccountCacheKeyIosService = configJson.GetValue("account_cache_key_ios_service").ToString();
                _expectedAccountCacheKeyIosAccount = configJson.GetValue("account_cache_key_ios_account").ToString();
                _expectedAccountCacheKeyIosGeneric = configJson.GetValue("account_cache_key_ios_generic").ToString();
                _expectedAccountCacheValue = configJson.GetValue("account_cache_value").ToString();

                var idTokenSecret = CreateIdToken(_idTokenResponse);

                _tokenResponse = string.Format
                    (CultureInfo.InvariantCulture, "{" + _tokenResponse + "}", idTokenSecret);

                _expectedIdTokenCacheValue = string.Format
                    (CultureInfo.InvariantCulture, "{" + _expectedIdTokenCacheValue + "}", idTokenSecret);
            }
        }

        public static string CreateIdToken(string idToken)
        {
            return string.Format
                (CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(idToken));
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void AAD_CacheFormatValidationTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                IntitTestData("AADTestData.txt");
                Init(harness.HttpManager);
                RunCacheFormatValidation(harness);
            }
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void MSA_CacheFormatValidationTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                IntitTestData("MSATestData.txt");
                Init(harness.HttpManager);
                RunCacheFormatValidation(harness);
            }
        }

        [TestMethod]
        [Description("Test unified token cache")]
        [Ignore] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1037
        public void B2C_NoTenantId_CacheFormatValidationTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                TestCommon.ResetInternalStaticCaches();
                IntitTestData("B2CNoTenantIdTestData.txt");
                RunCacheFormatValidation(harness);
            }
        }

        [TestMethod]
        [Description("Test unified token cache")]
        [Ignore]
        // it is not yet decided what version of tenant id should be used
        // test data generated based on GUID, Msal uses tenantId from passed in authotiry
        public void B2C_WithTenantId_CacheFormatValidationTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                IntitTestData("B2CWithTenantIdTestData.txt");
                RunCacheFormatValidation(harness);
            }
        }

        private void RunCacheFormatValidation(MockHttpAndServiceBundle harness)
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                                          .Create(_clientId)
                                          .WithAuthority(new Uri(_requestAuthority), true)
                                          .WithHttpManager(harness.HttpManager)
                                          .BuildConcrete();

            MsalMockHelpers.ConfigureMockWebUI(
                app.ServiceBundle.PlatformProxy,
                new AuthorizationResult(AuthorizationStatus.Success,
                app.AppConfig.RedirectUri + "?code=some-code"));

            //add mock response for tenant endpoint discovery
            harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.AuthorityHomeTenant)
            });
            harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(_tokenResponse)
            });

            AuthenticationResult result = app
                .AcquireTokenInteractive(MsalTestConstants.Scope)
                .ExecuteAsync(CancellationToken.None)
                .Result;

            Assert.IsNotNull(result);

            ValidateAt(app.UserTokenCacheInternal);
            ValidateRt(app.UserTokenCacheInternal);
            ValidateIdToken(app.UserTokenCacheInternal);
            ValidateAccount(app.UserTokenCacheInternal);
        }

        private void ValidateAt(ITokenCacheInternal cache)
        {
            var atList = cache.GetAllAccessTokens(false).ToList();
            Assert.AreEqual(1, atList.Count);

            var actualPayload = JsonConvert.DeserializeObject<JObject>(atList.First().ToJsonString());
            var expectedPayload = JsonConvert.DeserializeObject<JObject>(_expectedAtCacheValue);

            foreach (KeyValuePair<string, JToken> prop in expectedPayload)
            {
                string[] timeProperties = { "extended_expires_on", "expires_on", "cached_at" };

                var propName = prop.Key;
                var expectedPropValue = prop.Value;
                var actualPropValue = actualPayload.GetValue(propName);
                if (timeProperties.Contains(propName))
                {
                    if (!"extended_expires_on".Equals(propName))
                    {
                        Assert.IsTrue(actualPayload.GetValue(propName).Type == JTokenType.String);
                    }
                }
                else
                {
                    Assert.AreEqual(expectedPropValue, actualPropValue);
                }
            }
            var atCacheItem = cache.GetAllAccessTokens(true).First();
            var key = atCacheItem.GetKey();

            Assert.AreEqual(_expectedAtCacheKey, key.ToString());

            Assert.AreEqual(_expectedAtCacheKeyIosService, key.iOSService);
            Assert.AreEqual(_expectedAtCacheKeyIosAccount, key.iOSAccount);
            Assert.AreEqual(_expectedAtCacheKeyIosGeneric, key.iOSGeneric);
            Assert.AreEqual(_expectedAtCacheKeyIosGeneric, key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, key.iOSType);
        }

        private void ValidateRt(ITokenCacheInternal cache)
        {
            // TODO: NEED TO LOOK INTO HOW TO HANDLE THIS TEST
            //ValidateCacheEntityValue
            //    (ExpectedRtCacheValue, cache.GetAllRefreshTokenCacheItems(requestContext));

            var rtCacheItem = cache.GetAllRefreshTokens(true).First();
            var key = rtCacheItem.GetKey();

            Assert.AreEqual(_expectedRtCacheKey, key.ToString());

            Assert.AreEqual(_expectedRtCacheKeyIosService, key.iOSService);
            Assert.AreEqual(_expectedRtCacheKeyIosAccount, key.iOSAccount);
            Assert.AreEqual(_expectedRtCacheKeyIosGeneric, key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, key.iOSType);
        }

        private void ValidateIdToken(ITokenCacheInternal cache)
        {
            // TODO: NEED TO LOOK INTO HOW TO HANDLE THIS TEST
            //ValidateCacheEntityValue
            //    (ExpectedIdTokenCacheValue, cache.GetAllIdTokenCacheItems(requestContext));

            var idTokenCacheItem = cache.GetAllIdTokens(true).First();
            var key = idTokenCacheItem.GetKey();

            Assert.AreEqual(_expectedIdTokenCacheKey, key.ToString());

            Assert.AreEqual(_expectedIdTokenCacheKeyIosService, key.iOSService);
            Assert.AreEqual(_expectedIdTokenCacheKeyIosAccount, key.iOSAccount);
            Assert.AreEqual(_expectedIdTokenCacheKeyIosGeneric, key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.IdToken, key.iOSType);
        }

        private void ValidateAccount(ITokenCacheInternal cache)
        {
            // TODO: NEED TO LOOK INTO HOW TO HANDLE THIS TEST
            //ValidateCacheEntityValue
            //    (ExpectedAccountCacheValue, cache.GetAllAccountCacheItems(requestContext));

            var accountCacheItem = cache.GetAllAccounts().First();
            var key = accountCacheItem.GetKey();

            Assert.AreEqual(_expectedAccountCacheKey, key.ToString());

            Assert.AreEqual(_expectedAccountCacheKeyIosService, key.iOSService);
            Assert.AreEqual(_expectedAccountCacheKeyIosAccount, key.iOSAccount);
            Assert.AreEqual(_expectedAccountCacheKeyIosGeneric, key.iOSGeneric);
            Assert.AreEqual(MsalCacheKeys.iOSAuthorityTypeToAttrType["MSSTS"], key.iOSType);
        }

        private void ValidateCacheEntityValue(string expectedEntityValue, ICollection<string> entities)
        {
            Assert.IsTrue(entities.Count == 1);

            var actualPayload = JsonConvert.DeserializeObject<JObject>(entities.First());
            var expectedPayload = JsonConvert.DeserializeObject<JObject>(expectedEntityValue);

            Assert.AreEqual(expectedPayload.Count, actualPayload.Count);

            foreach (KeyValuePair<string, JToken> prop in expectedPayload)
            {
                var propName = prop.Key;
                var expectedPropValue = prop.Value;
                var actualPropValue = actualPayload.GetValue(propName);

                Assert.AreEqual(expectedPropValue, actualPropValue);
            }
        }
    }
}
#endif
