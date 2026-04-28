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
using System.Text.Json;
using System.Text.Json.Nodes;

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
                var configJson = JsonNode.Parse(json).AsObject();

                _clientId = configJson["client_id"]?.GetValue<string>();
                _requestAuthority = configJson["authority"]?.GetValue<string>();

                _tokenResponse = configJson["token_response"]?.GetValue<string>();
                _idTokenResponse = configJson["id_token_response"]?.GetValue<string>();

                _expectedAtCacheKey = configJson["at_cache_key"]?.GetValue<string>();
                _expectedAtCacheKeyIosService = configJson["at_cache_key_ios_service"]?.GetValue<string>();
                _expectedAtCacheKeyIosAccount = configJson["at_cache_key_ios_account"]?.GetValue<string>();
                _expectedAtCacheKeyIosGeneric = configJson["at_cache_key_ios_generic"]?.GetValue<string>();
                _expectedAtCacheKey = configJson["at_cache_key"]?.GetValue<string>();

                _expectedAtCacheValue = configJson["at_cache_value"]?.GetValue<string>();

                _expectedIdTokenCacheKey = configJson["id_token_cache_key"]?.GetValue<string>();
                _expectedIdTokenCacheKeyIosService = configJson["id_token_cache_key_ios_service"]?.GetValue<string>();
                _expectedIdTokenCacheKeyIosAccount = configJson["id_token_cache_key_ios_account"]?.GetValue<string>();
                _expectedIdTokenCacheKeyIosGeneric = configJson["id_token_cache_key_ios_generic"]?.GetValue<string>();
                _expectedIdTokenCacheValue = configJson["id_token_cache_value"]?.GetValue<string>();

                _expectedRtCacheKey = configJson["rt_cache_key"]?.GetValue<string>();
                _expectedRtCacheKeyIosService = configJson["rt_cache_key_ios_service"]?.GetValue<string>();
                _expectedRtCacheKeyIosAccount = configJson["rt_cache_key_ios_account"]?.GetValue<string>();
                _expectedRtCacheKeyIosGeneric = configJson["rt_cache_key_ios_generic"]?.GetValue<string>();
                _expectedRtCacheValue = configJson["rt_cache_value"]?.GetValue<string>();

                _expectedAccountCacheKey = configJson["account_cache_key"]?.GetValue<string>();
                _expectedAccountCacheKeyIosService = configJson["account_cache_key_ios_service"]?.GetValue<string>();
                _expectedAccountCacheKeyIosAccount = configJson["account_cache_key_ios_account"]?.GetValue<string>();
                _expectedAccountCacheKeyIosGeneric = configJson["account_cache_key_ios_generic"]?.GetValue<string>();
                _expectedAccountCacheValue = configJson["account_cache_value"]?.GetValue<string>();

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
                ApplicationBase.ResetStateForTest();
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
            Assert.HasCount(1, atList);

            var actualPayload = JsonNode.Parse(atList.First().ToJsonString()).AsObject();
            var expectedPayload = JsonNode.Parse(_expectedAtCacheValue).AsObject();

            foreach (var prop in expectedPayload)
            {
                string[] timeProperties = { "extended_expires_on", "expires_on", "cached_at" };

                var propName = prop.Key;
                var expectedPropValue = prop.Value;
                var actualPropValue = actualPayload[propName];
                if (timeProperties.Contains(propName))
                {
                    if (!"extended_expires_on".Equals(propName))
                    {
                        Assert.IsNotNull(actualPayload[propName]?.GetValue<string>());
                    }
                }
                else
                {
                    Assert.AreEqual(expectedPropValue?.ToJsonString(), actualPropValue?.ToJsonString());
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
            Assert.HasCount(1, entities);

            var actualPayload = JsonNode.Parse(entities.First()).AsObject();
            var expectedPayload = JsonNode.Parse(expectedEntityValue).AsObject();

            Assert.AreEqual(expectedPayload.Count, actualPayload.Count);

            foreach (var prop in expectedPayload)
            {
                var propName = prop.Key;
                var expectedPropValue = prop.Value;
                var actualPropValue = actualPayload[propName];

                Assert.AreEqual(expectedPropValue?.ToJsonString(), actualPropValue?.ToJsonString());
            }
        }
    }
}
