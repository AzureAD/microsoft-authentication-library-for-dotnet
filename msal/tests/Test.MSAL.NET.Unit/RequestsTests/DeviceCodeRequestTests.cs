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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Features.DeviceCode;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    [TestClass]
    public class DeviceCodeRequestTests
    {
        private TokenCache _cache;
        private readonly MyReceiver _myReceiver = new MyReceiver();

        [TestInitialize]
        public void TestInitialize()
        {
            RequestTestsCommon.InitializeRequestTests();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);

            _cache = new TokenCache();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.tokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            _cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
        }

        private const string ExpectedDeviceCode = "BAQABAAEAAADXzZ3ifr-GRbDT45zNSEFEfU4P-bZYS1vkvv8xiXdb1_zX2xAcdcfEoei1o-t9-zTB9sWyTcddFEWahP1FJJJ_YVA1zvPM2sV56d_8O5G23ti5uu0nRbIsniczabYYEr-2ZsbgRO62oZjKlB1zF3EkuORg2QhMOjtsk-KP0aw8_iAA";
        private const string ExpectedUserCode = "B6SUYU5PL";
        private const int ExpectedExpiresIn = 900;
        private const int ExpectedInterval = 5;
        private const string ExpectedVerificationUrl = "https://microsoft.com/devicelogin";

        private string ExpectedMessage =>
            $"To sign in, use a web browser to open the page {ExpectedVerificationUrl} and enter the code {ExpectedUserCode} to authenticate.";

        private string ExpectedResponseMessage =>
            $"{{" +
            $"\"user_code\":\"{ExpectedUserCode}\"," +
            $"\"device_code\":\"{ExpectedDeviceCode}\"," +
            $"\"verification_url\":\"{ExpectedVerificationUrl}\"," +
            $"\"expires_in\":\"{ExpectedExpiresIn}\"," +
            $"\"interval\":\"{ExpectedInterval}\"," +
            $"\"message\":\"{ExpectedMessage}\"," +
            $"}}";

        private HttpResponseMessage CreateDeviceCodeResponseSuccessMessage()
        {
            return MockHelpers.CreateSuccessResponseMessage(ExpectedResponseMessage);
        }

        [TestMethod]
        [TestCategory("DeviceCodeRequestTests")]
        public void TestDeviceCodeAuthSuccess()
        {
            var parameters = CreateAuthenticationParametersAndSetupMocks(out HashSet<string> expectedScopes);

            // Check that cache is empty
            Assert.AreEqual(0, _cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, _cache.tokenCacheAccessor.AccountCacheDictionary.Count);
            Assert.AreEqual(0, _cache.tokenCacheAccessor.IdTokenCacheDictionary.Count);
            Assert.AreEqual(0, _cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);

            DeviceCodeResult actualDeviceCodeResult = null;
            DeviceCodeRequest request = new DeviceCodeRequest(parameters, result => 
            {
                actualDeviceCodeResult = result;
                return Task.FromResult(0);
            });
            var task = request.RunAsync(CancellationToken.None);
            task.Wait();
            var authenticationResult = task.Result;
            Assert.IsNotNull(authenticationResult);
            Assert.IsNotNull(actualDeviceCodeResult);

            Assert.AreEqual(TestConstants.ClientId, actualDeviceCodeResult.ClientId);
            Assert.AreEqual(ExpectedDeviceCode, actualDeviceCodeResult.DeviceCode);
            Assert.AreEqual(ExpectedInterval, actualDeviceCodeResult.Interval);
            Assert.AreEqual(ExpectedMessage, actualDeviceCodeResult.Message);
            Assert.AreEqual(ExpectedUserCode, actualDeviceCodeResult.UserCode);
            Assert.AreEqual(ExpectedVerificationUrl, actualDeviceCodeResult.VerificationUrl);

            CoreAssert.AreScopesEqual(expectedScopes.AsSingleString(), actualDeviceCodeResult.Scopes.AsSingleString());

            // Validate that entries were added to cache
            Assert.AreEqual(1, _cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(1, _cache.tokenCacheAccessor.AccountCacheDictionary.Count);
            Assert.AreEqual(1, _cache.tokenCacheAccessor.IdTokenCacheDictionary.Count);
            Assert.AreEqual(1, _cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("DeviceCodeRequestTests")]
        public void TestDeviceCodeCancel()
        {
            var parameters = CreateAuthenticationParametersAndSetupMocks(out HashSet<string> expectedScopes);

            CancellationTokenSource cancellationSource = new CancellationTokenSource();

            DeviceCodeResult actualDeviceCodeResult = null;
            DeviceCodeRequest request = new DeviceCodeRequest(parameters, async result =>
            {
                await Task.Delay(200);
                actualDeviceCodeResult = result;
            });

            // We setup the cancel before calling the RunAsync operation since we don't check the cancel
            // until later and the mock network calls run insanely fast for us to timeout for them.
            cancellationSource.Cancel();
            AssertException.TaskThrows<OperationCanceledException>(() => request.RunAsync(cancellationSource.Token));
        }

        private AuthenticationRequestParameters CreateAuthenticationParametersAndSetupMocks(out HashSet<string> expectedScopes)
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                TokenCache = _cache,
                RequestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null))
            };

            RequestTestsCommon.MockInstanceDiscoveryAndOpenIdRequest();

            expectedScopes = new HashSet<string>();
            expectedScopes.UnionWith(TestConstants.Scope);
            expectedScopes.Add(OAuth2Value.ScopeOfflineAccess);
            expectedScopes.Add(OAuth2Value.ScopeProfile);
            expectedScopes.Add(OAuth2Value.ScopeOpenId);

            // Mock Handler for device code request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                PostData = new Dictionary<string, string>()
                {
                    {OAuth2Parameter.ClientId, TestConstants.ClientId},
                    {OAuth2Parameter.Scope, expectedScopes.AsSingleString()}
                },
                ResponseMessage = CreateDeviceCodeResponseSuccessMessage()
            });

            // Mock Handler for devicecode->token exchange request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                PostData = new Dictionary<string, string>()
                {
                    {OAuth2Parameter.ClientId, TestConstants.ClientId},
                    {OAuth2Parameter.Scope, expectedScopes.AsSingleString()}
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            return parameters;
        }
    }
}
