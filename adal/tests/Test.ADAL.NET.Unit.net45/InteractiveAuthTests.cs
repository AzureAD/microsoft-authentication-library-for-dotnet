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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;
using PromptBehavior = Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior;
using Microsoft.Identity.Core.UI;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using System.Linq;
using Test.Microsoft.Identity.Core.Unit;


#if !NET_CORE
namespace Test.ADAL.NET.Unit.net45
{
    [TestClass]
    public class InteractiveAuthTests
    {
        private PlatformParameters _platformParameters;
        private AuthenticationContext _context;

        [TestInitialize]
        public void Initialize()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();

            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant)));
            _platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            _context?.TokenCache?.Clear();
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNet")]
        public async Task SmokeTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, true);
            AuthenticationResult result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.IsTrue(_context.Authenticator.Authority.EndsWith("/some-tenant-id/"));
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(result.ExpiresOn, result.ExtendedExpiresOn);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(AdalTestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken with extended expires on support")]
        [TestCategory("AdalDotNet")]
        public async Task SmokeTestWithExtendedExpiresOnAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(true)
            });

            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, true);
            AuthenticationResult result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.IsTrue(_context.Authenticator.Authority.EndsWith("/some-tenant-id/"));
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.IsTrue(result.ExtendedExpiresOn.Subtract(result.ExpiresOn) > TimeSpan.FromSeconds(5));
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(AdalTestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Positive Test for ExtendedLife Feature")]
        [TestCategory("AdalDotNet")]
        public async Task ExtendedLifetimeRetryAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                 AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, true);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout),
            });

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
            });

            _context.ExtendedLifeTimeEnabled = true;
            AuthenticationResult result =
            await _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);

            _context.TokenCache.Clear();
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());

        }


        [TestMethod]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        public async Task AcquireTokenPositiveWithoutUserIdAsync()
        {
            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            var exc = AssertException.TaskThrows<ArgumentException>(() =>
                _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                    AdalTestConstants.DefaultRedirectUri, _platformParameters, null));
            Assert.IsTrue(exc.Message.StartsWith(AdalErrorMessage.SpecifyAnyUser));


            // this should hit the cache
            result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters, UserIdentifier.AnyUser).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            // There should be one cached entry.
            Assert.AreEqual(1, _context.TokenCache.Count);
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            _context.TokenCache.Clear();
        }

        [TestMethod]
        [Description("Test for authority validation to AuthenticationContext")]
        public async Task AuthenticationContextAuthorityValidationTestAsync()
        {
            _context = null;
            AuthenticationResult result = null;

            var ex = AssertException.Throws<ArgumentException>(() => new AuthenticationContext("https://login.contoso.com/adfs"));
            Assert.AreEqual(ex.ParamName, "validateAuthority");


            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            //whitelisted authority
            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, true, new TokenCache());
            _context.TokenCache.Clear(); // need to reset cache before starting utest.
            result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters,
                        new UserIdentifier(AdalTestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId)).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);

            // There should be one cached entry.
            Assert.AreEqual(1, _context.TokenCache.Count, "Number of items in the cache is not as expected.");

            //add handler to return failed discovery response
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Get,
                ResponseMessage =
                    MockHelpers.CreateFailureResponseMessage(
                        "{\"error\":\"invalid_instance\",\"error_description\":\"AADSTS70002: Error in validating authority.\"}")
            });

            _context = new AuthenticationContext("https://login.microsoft0nline.com/common");
            var adalEx = AssertException.TaskThrows<AdalException>(() =>
                _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                    AdalTestConstants.DefaultRedirectUri, _platformParameters));

            Assert.AreEqual(adalEx.ErrorCode, AdalError.AuthorityNotInValidList);
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            _context.TokenCache.Clear();
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        public void AcquireTokenWithAuthenticationCanceledTest()
        {
            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.UserCancel,
                AdalTestConstants.DefaultRedirectUri + "?error=user_cancelled"));

            var exc = AssertException.TaskThrows<AdalServiceException>(() =>
                _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters));

            Assert.AreEqual(exc.ErrorCode, AdalError.AuthenticationCanceled);

            // There should be no cached entries.
            Assert.AreEqual(0, _context.TokenCache.Count);
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        public async Task AcquireTokenPositiveWithNullCacheTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, null);
            AuthenticationResult result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Test for acquiring token using tenant specific endpoint")]
        public async Task TenantSpecificAuthorityTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, true);
            AuthenticationResult result =
                await
                    _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.AreEqual(AdalTestConstants.DefaultAuthorityHomeTenant, _context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(AdalTestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Test for ensuring ADAL returns the appropriate headers during a http failure.")]
        public async Task HttpErrorResponseWithHeadersAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                                           AdalTestConstants.DefaultRedirectUri + "?code=some-code"));

            List<KeyValuePair<string, string>> HttpErrorResponseWithHeaders = new List<KeyValuePair<string, string>>();
            HttpErrorResponseWithHeaders.Add(new KeyValuePair<string, string>("Retry-After", "120"));
            HttpErrorResponseWithHeaders.Add(new KeyValuePair<string, string>("GatewayTimeout", "0"));
            HttpErrorResponseWithHeaders.Add(new KeyValuePair<string, string>("Forbidden", "0"));

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.DefaultAuthorityCommonTenant)
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateCustomHeaderFailureResponseMessage(HttpErrorResponseWithHeaders)
            });

            _context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, true);

            try
            {
                AuthenticationResult result = await _context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                                              AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (ex is AdalServiceException adalEx)
                {
                    foreach (KeyValuePair<string, string> header in HttpErrorResponseWithHeaders)
                    {
                        var match = adalEx.Headers.Where(x => x.Key == header.Key && x.Value.Contains(header.Value)).FirstOrDefault();
                        Assert.IsNotNull(match);
                    }
                }
            }
            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            _context.TokenCache.Clear();
        }
    }
}

#endif
