// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    [DeploymentItem(@"Resources\AADTestData.txt")]
    [DeploymentItem(@"Resources\MSATestData.txt")]
    [DeploymentItem(@"Resources\B2CNoTenantIdTestData.txt")]
    [DeploymentItem(@"Resources\B2CWithTenantIdTestData.txt")]
    public class UnifiedCacheFormatTests : TestBase
    {
        private void Init(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
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
            using (var harness = CreateTestHarness())
            {
                IntitTestData(ResourceHelper.GetTestResourceRelativePath("AADTestData.txt"));
                Init(harness.HttpManager);

                RunCacheFormatValidation(harness);
            }
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void MSA_CacheFormatValidationTest()
        {
            using (var harness = CreateTestHarness())
            {
                IntitTestData(ResourceHelper.GetTestResourceRelativePath("MSATestData.txt"));
                Init(harness.HttpManager);

                RunCacheFormatValidation(harness);
            }
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void B2C_NoTenantId_CacheFormatValidationTest()
        {
            using (var harness = CreateTestHarness())
            {
                TestCommon.ResetInternalStaticCaches();
                IntitTestData(ResourceHelper.GetTestResourceRelativePath("B2CNoTenantIdTestData.txt"));
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

            app.ServiceBundle.ConfigureMockWebUI(
                AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

            harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(_tokenResponse)
            });

            AuthenticationResult result = app
                .AcquireTokenInteractive(TestConstants.s_scope)
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
            var atList = cache.Accessor.GetAllAccessTokens().ToList();
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
            var atCacheItem = cache.Accessor.GetAllAccessTokens().First();
            var keyString = atCacheItem.CacheKey;
            var iOsKey = atCacheItem.iOSCacheKey;

            Assert.AreEqual(_expectedAtCacheKey, keyString);

            Assert.AreEqual(_expectedAtCacheKeyIosService, iOsKey.iOSService);
            Assert.AreEqual(_expectedAtCacheKeyIosAccount, iOsKey.iOSAccount);
            Assert.AreEqual(_expectedAtCacheKeyIosGeneric, iOsKey.iOSGeneric);
            Assert.AreEqual(_expectedAtCacheKeyIosGeneric, iOsKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, iOsKey.iOSType);
        }

        private void ValidateRt(ITokenCacheInternal cache)
        {
            // TODO: NEED TO LOOK INTO HOW TO HANDLE THIS TEST
            //ValidateCacheEntityValue
            //    (ExpectedRtCacheValue, cache.GetAllRefreshTokenCacheItems(requestContext));

            var rtCacheItem = cache.Accessor.GetAllRefreshTokens().First();
            var iOSKey = rtCacheItem.iOSCacheKey;

            Assert.AreEqual(_expectedRtCacheKey, rtCacheItem.CacheKey);

            Assert.AreEqual(_expectedRtCacheKeyIosService, iOSKey.iOSService);
            Assert.AreEqual(_expectedRtCacheKeyIosAccount, iOSKey.iOSAccount);
            Assert.AreEqual(_expectedRtCacheKeyIosGeneric, iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, iOSKey.iOSType);
        }

        private void ValidateIdToken(ITokenCacheInternal cache)
        {
            // TODO: NEED TO LOOK INTO HOW TO HANDLE THIS TEST
            //ValidateCacheEntityValue
            //    (ExpectedIdTokenCacheValue, cache.GetAllIdTokenCacheItems(requestContext));

            var idTokenCacheItem = cache.Accessor.GetAllIdTokens().First();
            var key = idTokenCacheItem.iOSCacheKey;

            Assert.AreEqual(_expectedIdTokenCacheKey, idTokenCacheItem.CacheKey);

            Assert.AreEqual(_expectedIdTokenCacheKeyIosService, key.iOSService);
            Assert.AreEqual(_expectedIdTokenCacheKeyIosAccount, key.iOSAccount);
            Assert.AreEqual(_expectedIdTokenCacheKeyIosGeneric, key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.IdToken, key.iOSType);
        }

        private void ValidateAccount(ITokenCacheInternal cache)
        {
            var accountCacheItem = cache.Accessor.GetAllAccounts().First();
            var iOSKey = accountCacheItem.iOSCacheKey;

            Assert.AreEqual(_expectedAccountCacheKey, accountCacheItem.CacheKey);

            Assert.AreEqual(_expectedAccountCacheKeyIosService, iOSKey.iOSService);
            Assert.AreEqual(_expectedAccountCacheKeyIosAccount, iOSKey.iOSAccount);
            Assert.AreEqual(MsalCacheKeys.iOSAuthorityTypeToAttrType["MSSTS"], iOSKey.iOSType);
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
