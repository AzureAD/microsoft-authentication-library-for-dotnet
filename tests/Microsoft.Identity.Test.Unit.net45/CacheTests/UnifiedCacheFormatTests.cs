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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Instance;
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
        private void TestInitialize(MockHttpManager httpManager)
        {
            TestCommon.ResetStateAndInitMsal();

            httpManager.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    MsalTestConstants.GetDiscoveryEndpoint(MsalTestConstants.AuthorityCommonTenant)));
        }

        private string ClientId;
        private string RequestAuthority;

        private string TokenResponse;
        private string IdTokenResponse;

        private string ExpectedAtCacheKey;
        private string ExpectedAtCacheKeyIosService;
        private string ExpectedAtCacheKeyIosAccount;
        private string ExpectedAtCacheKeyIosGeneric;
        private string ExpectedAtCacheValue;

        private string ExpectedIdTokenCacheKey;
        private string ExpectedIdTokenCacheKeyIosService;
        private string ExpectedIdTokenCacheKeyIosAccount;
        private string ExpectedIdTokenCacheKeyIosGeneric;
        private string ExpectedIdTokenCacheValue;

        private string ExpectedRtCacheKey;
        private string ExpectedRtCacheKeyIosService;
        private string ExpectedRtCacheKeyIosAccount;
        private string ExpectedRtCacheKeyIosGeneric;
        private string ExpectedRtCacheValue;

        private string ExpectedAccountCacheKey;
        private string ExpectedAccountCacheKeyIosService;
        private string ExpectedAccountCacheKeyIosAccount;
        private string ExpectedAccountCacheKeyIosGeneric;
        private string ExpectedAccountCacheValue;

        private readonly RequestContext requestContext = RequestContext.CreateForTest();

        private void IntitTestData(string fileName)
        {
            using (StreamReader r = new StreamReader(fileName))
            {
                string json = r.ReadToEnd();
                var configJson = JsonConvert.DeserializeObject<JObject>(json);

                ClientId = configJson.GetValue("client_id").ToString();
                RequestAuthority = configJson.GetValue("authority").ToString();

                TokenResponse = configJson.GetValue("token_response").ToString();
                IdTokenResponse = configJson.GetValue("id_token_response").ToString();

                ExpectedAtCacheKey = configJson.GetValue("at_cache_key").ToString();
                ExpectedAtCacheKeyIosService = configJson.GetValue("at_cache_key_ios_service").ToString();
                ExpectedAtCacheKeyIosAccount = configJson.GetValue("at_cache_key_ios_account").ToString();
                ExpectedAtCacheKeyIosGeneric = configJson.GetValue("at_cache_key_ios_generic").ToString();
                ExpectedAtCacheKey = configJson.GetValue("at_cache_key").ToString();

                ExpectedAtCacheValue = configJson.GetValue("at_cache_value").ToString();

                ExpectedIdTokenCacheKey = configJson.GetValue("id_token_cache_key").ToString();
                ExpectedIdTokenCacheKeyIosService = configJson.GetValue("id_token_cache_key_ios_service").ToString();
                ExpectedIdTokenCacheKeyIosAccount = configJson.GetValue("id_token_cache_key_ios_account").ToString();
                ExpectedIdTokenCacheKeyIosGeneric = configJson.GetValue("id_token_cache_key_ios_generic").ToString();
                ExpectedIdTokenCacheValue = configJson.GetValue("id_token_cache_value").ToString();

                ExpectedRtCacheKey = configJson.GetValue("rt_cache_key").ToString();
                ExpectedRtCacheKeyIosService = configJson.GetValue("rt_cache_key_ios_service").ToString();
                ExpectedRtCacheKeyIosAccount = configJson.GetValue("rt_cache_key_ios_account").ToString();
                ExpectedRtCacheKeyIosGeneric = configJson.GetValue("rt_cache_key_ios_generic").ToString();
                ExpectedRtCacheValue = configJson.GetValue("rt_cache_value").ToString();

                ExpectedAccountCacheKey = configJson.GetValue("account_cache_key").ToString();
                ExpectedAccountCacheKeyIosService = configJson.GetValue("account_cache_key_ios_service").ToString();
                ExpectedAccountCacheKeyIosAccount = configJson.GetValue("account_cache_key_ios_account").ToString();
                ExpectedAccountCacheKeyIosGeneric = configJson.GetValue("account_cache_key_ios_generic").ToString();
                ExpectedAccountCacheValue = configJson.GetValue("account_cache_value").ToString();

                var idTokenSecret = CreateIdToken(IdTokenResponse);

                TokenResponse = string.Format
                    (CultureInfo.InvariantCulture, "{" + TokenResponse + "}", idTokenSecret);

                ExpectedIdTokenCacheValue = string.Format
                    (CultureInfo.InvariantCulture, "{" + ExpectedIdTokenCacheValue + "}", idTokenSecret);
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
            IntitTestData("AADTestData.txt");
            RunCacheFormatValidation();
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void MSA_CacheFormatValidationTest()
        {
            IntitTestData("MSATestData.txt");
            RunCacheFormatValidation();
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void B2C_NoTenantId_CacheFormatValidationTest()
        {
            IntitTestData("B2CNoTenantIdTestData.txt");
            RunCacheFormatValidation();
        }

        [TestMethod]
        [Description("Test unified token cache")]
        [Ignore]
        // it is not yet decided what version of tenant id should be used
        // test data generated based on GUID, Msal uses tenantId from passed in authotiry
        public void B2C_WithTenantId_CacheFormatValidationTest()
        {
            IntitTestData("B2CWithTenantIdTestData.txt");
            RunCacheFormatValidation();
        }

        public void RunCacheFormatValidation()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                TestInitialize(harness.HttpManager);

                PublicClientApplication app = PublicClientApplicationBuilder
                                              .Create(ClientId)
                                              .WithAuthority(new Uri(RequestAuthority), true)
                                              .WithHttpManager(harness.HttpManager)
                                              .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success,
                    app.AppConfig.RedirectUri + "?code=some-code"));

                //add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    Method = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.AuthorityHomeTenant)
                });
                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessResponseMessage(TokenResponse)
                });

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);

                ValidateAt(app.UserTokenCacheInternal);
                ValidateRt(app.UserTokenCacheInternal);
                ValidateIdToken(app.UserTokenCacheInternal);
                ValidateAccount(app.UserTokenCacheInternal);
            }
        }

        private void ValidateAt(ITokenCacheInternal cache)
        {
            var atList = cache.GetAllAccessTokenCacheItems(requestContext);
            Assert.IsTrue(atList.Count == 1);

            var actualPayload = JsonConvert.DeserializeObject<JObject>(atList.First());
            var expectedPayload = JsonConvert.DeserializeObject<JObject>(ExpectedAtCacheValue);

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
            var atCacheItem = cache.GetAllAccessTokensForClient(requestContext).First();
            var key = atCacheItem.GetKey();

            Assert.AreEqual(ExpectedAtCacheKey, key.ToString());

            Assert.AreEqual(ExpectedAtCacheKeyIosService, key.GetiOSServiceKey());
            Assert.AreEqual(ExpectedAtCacheKeyIosAccount, key.GetiOSAccountKey());
            Assert.AreEqual(ExpectedAtCacheKeyIosGeneric, key.GetiOSGenericKey());
        }

        private void ValidateRt(ITokenCacheInternal cache)
        {
            ValidateCacheEntityValue
                (ExpectedRtCacheValue, cache.GetAllRefreshTokenCacheItems(requestContext));

            var rtCacheItem = cache.GetAllRefreshTokensForClient(requestContext).First();
            var key = rtCacheItem.GetKey();

            Assert.AreEqual(ExpectedRtCacheKey, key.ToString());

            Assert.AreEqual(ExpectedRtCacheKeyIosService, key.GetiOSServiceKey());
            Assert.AreEqual(ExpectedRtCacheKeyIosAccount, key.GetiOSAccountKey());
            Assert.AreEqual(ExpectedRtCacheKeyIosGeneric, key.GetiOSGenericKey());
        }

        private void ValidateIdToken(ITokenCacheInternal cache)
        {
            ValidateCacheEntityValue
                (ExpectedIdTokenCacheValue, cache.GetAllIdTokenCacheItems(requestContext));

            var idTokenCacheItem = cache.GetAllIdTokensForClient(requestContext).First();
            var key = idTokenCacheItem.GetKey();

            Assert.AreEqual(ExpectedIdTokenCacheKey, key.ToString());

            Assert.AreEqual(ExpectedIdTokenCacheKeyIosService, key.GetiOSServiceKey());
            Assert.AreEqual(ExpectedIdTokenCacheKeyIosAccount, key.GetiOSAccountKey());
            Assert.AreEqual(ExpectedIdTokenCacheKeyIosGeneric, key.GetiOSGenericKey());
        }

        private void ValidateAccount(ITokenCacheInternal cache)
        {
            ValidateCacheEntityValue
                (ExpectedAccountCacheValue, cache.GetAllAccountCacheItems(requestContext));

            var accountCacheItem = cache.GetAllAccounts(requestContext).First();
            var key = accountCacheItem.GetKey();

            Assert.AreEqual(ExpectedAccountCacheKey, key.ToString());

            Assert.AreEqual(ExpectedAccountCacheKeyIosService, key.GetiOSServiceKey());
            Assert.AreEqual(ExpectedAccountCacheKeyIosAccount, key.GetiOSAccountKey());
            Assert.AreEqual(ExpectedAccountCacheKeyIosGeneric, key.GetiOSGenericKey());
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