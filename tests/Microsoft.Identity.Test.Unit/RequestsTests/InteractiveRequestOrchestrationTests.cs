// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestOrchestrationTests : TestBase
    {
        private IAuthCodeRequestComponent _authCodeRequestComponentOverride;
        private ITokenRequestComponent _authCodeExchangeComponentOverride;
        private ITokenRequestComponent _brokerExchangeComponentOverride;
        private readonly MsalTokenResponse _msalTokenResponse = TestConstants.CreateMsalTokenResponse();
        private readonly MsalTokenResponse _msalTokenResponseWithTokenSource = TestConstants.CreateMsalTokenResponseWithTokenSource();
        private const string AuthCodeWithAppLink = "msauth://wpj?username=joe@contoso.onmicrosoft.com&app_link=itms%3a%2f%2fitunes.apple.com%2fapp%2fazure-authenticator%2fid983156458%3fmt%3d8";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _authCodeRequestComponentOverride = Substitute.For<IAuthCodeRequestComponent>();
            _authCodeExchangeComponentOverride = Substitute.For<ITokenRequestComponent>();
            _brokerExchangeComponentOverride = Substitute.For<ITokenRequestComponent>();
        }

        [TestMethod]
        [Description(
            "Setup: Broker is not configured. Evo is ok with user logging-in with a web ui" +
            "Behavior: InteractiveRequest uses WebUI only. Auth Code is exchanged to token.")]
        public async Task NoBroker_WebUiOnly_Async()
        {
            // Arrange - common stuff
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscovery(harness.HttpManager);

                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache);
                var interactiveParameters = new AcquireTokenInteractiveParameters();

                // Arrange - important for test
                requestParams.AppConfig.IsBrokerEnabled = false;
                var authCodeResult = (new AuthorizationResult { Code= "some_auth_code" }, "pkce_verifier");
                _authCodeRequestComponentOverride.FetchAuthCodeAndPkceVerifierAsync(CancellationToken.None)
                    .Returns(Task.FromResult(authCodeResult));

                _authCodeExchangeComponentOverride.FetchTokensAsync(CancellationToken.None)
                    .Returns(Task.FromResult(_msalTokenResponse));

                InteractiveRequest interactiveRequest = new InteractiveRequest(
                       requestParams,
                       interactiveParameters,
                       _authCodeRequestComponentOverride,
                       _authCodeExchangeComponentOverride,
                       _brokerExchangeComponentOverride);

                // Act
                AuthenticationResult result = await interactiveRequest.RunAsync().ConfigureAwait(false);

                // Assert - common stuff
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
                Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count);

                // Assert - orchestration
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
                Received.InOrder(async () =>
                {
                    await _authCodeRequestComponentOverride
                        .FetchAuthCodeAndPkceVerifierAsync(default)
                        .ConfigureAwait(false);

                    await _authCodeExchangeComponentOverride
                       .FetchTokensAsync(default)
                       .ConfigureAwait(false);
                });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates

                await _brokerExchangeComponentOverride
                   .DidNotReceiveWithAnyArgs()
                   .FetchTokensAsync(default)
                   .ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Description(
         "Setup: Broker is configured and installed" +
         "Behavior: InteractiveRequest uses broker flow only")]
        public async Task Broker_Configured_And_Installed_Async()
        {
            // Arrange - common stuff
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscovery(harness.HttpManager);

                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache);
                var interactiveParameters = new AcquireTokenInteractiveParameters();

                // Arrange - important for test
                requestParams.AppConfig.IsBrokerEnabled = true;
                _brokerExchangeComponentOverride
                    .FetchTokensAsync(default)
                    .Returns(Task.FromResult(_msalTokenResponseWithTokenSource));

                InteractiveRequest interactiveRequest = new InteractiveRequest(
                       requestParams,
                       interactiveParameters,
                       _authCodeRequestComponentOverride,
                       _authCodeExchangeComponentOverride,
                       _brokerExchangeComponentOverride);

                // Act
                AuthenticationResult result = await interactiveRequest.RunAsync().ConfigureAwait(false);

                // Assert - common stuff
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Broker, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));

                // Assert - orchestration
                await _brokerExchangeComponentOverride
                   .Received(1)
                   .FetchTokensAsync(default)
                   .ConfigureAwait(false);

                await _authCodeRequestComponentOverride
                   .DidNotReceiveWithAnyArgs()
                   .FetchAuthCodeAndPkceVerifierAsync(default)
                   .ConfigureAwait(false);

                await _authCodeExchangeComponentOverride
                   .DidNotReceiveWithAnyArgs()
                   .FetchTokensAsync(default)
                   .ConfigureAwait(false);

            }
        }

        [TestMethod]
        [Description(
         "Setup: Broker is configured but not installed" +
         "Behavior: InteractiveRequest tries to use broker but reverts to web UI")]
        public async Task Broker_Configured_But_Not_Installed_Async()
        {
            // Arrange - common stuff
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscovery(harness.HttpManager);

                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache);
                var interactiveParameters = new AcquireTokenInteractiveParameters();

                // Arrange - important for test
                requestParams.AppConfig.IsBrokerEnabled = true;

                // broker returns null 
                _brokerExchangeComponentOverride
                    .FetchTokensAsync(default)
                    .Returns((MsalTokenResponse)null);

                // web UI can deal with this
                var authCodeResult = (new AuthorizationResult { Code = "some_auth_code" }, "pkce_verifier");
                _authCodeRequestComponentOverride.FetchAuthCodeAndPkceVerifierAsync(CancellationToken.None)
                    .Returns(Task.FromResult(authCodeResult));
                _authCodeExchangeComponentOverride.FetchTokensAsync(CancellationToken.None)
                    .Returns(Task.FromResult(_msalTokenResponse));

                InteractiveRequest interactiveRequest = new InteractiveRequest(
                       requestParams,
                       interactiveParameters,
                       _authCodeRequestComponentOverride,
                       _authCodeExchangeComponentOverride,
                       _brokerExchangeComponentOverride);

                // Act
                AuthenticationResult result = await interactiveRequest.RunAsync().ConfigureAwait(false);

                // Assert - common stuff
                Assert.IsNotNull(result);
                Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
                Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count);

                // Assert - orchestration

                // Assert - orchestration
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
                Received.InOrder(async () =>
                {
                    await _brokerExchangeComponentOverride
                        .FetchTokensAsync(default)
                        .ConfigureAwait(false);

                    await _authCodeRequestComponentOverride
                        .FetchAuthCodeAndPkceVerifierAsync(default)
                        .ConfigureAwait(false);

                    await _authCodeExchangeComponentOverride
                       .FetchTokensAsync(default)
                       .ConfigureAwait(false);
                });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
            }
        }

        [TestMethod]
        [Description(
        "Setup: Broker is NOT configured but is installed. EVO does not accept web UI authentication." +
        "Behavior: InteractiveRequest tries to use webUI, but reverts to using broker based on auth code.")]
        public async Task Broker_Not_Configured_But_Installed_EvoWantsBroker_Async()
        {
            // Arrange - common stuff
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscovery(harness.HttpManager);

                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache);
                var interactiveParameters = new AcquireTokenInteractiveParameters();

                // Arrange - important for test
                requestParams.AppConfig.IsBrokerEnabled = false;

                // web UI starts the flow, but the auth code shows Evo needs the broker
                var authCodeResult = (new AuthorizationResult { Code = AuthCodeWithAppLink }, "pkce_verifier");
                _authCodeRequestComponentOverride.FetchAuthCodeAndPkceVerifierAsync(CancellationToken.None)
                    .Returns(Task.FromResult(authCodeResult));

                // broker returns a response
                _brokerExchangeComponentOverride
                    .FetchTokensAsync(default)
                    .Returns(_msalTokenResponse);

                InteractiveRequest interactiveRequest = new InteractiveRequest(
                       requestParams,
                       interactiveParameters,
                       _authCodeRequestComponentOverride,
                       _authCodeExchangeComponentOverride,
                       _brokerExchangeComponentOverride);

                // Act
                AuthenticationResult result = await interactiveRequest.RunAsync().ConfigureAwait(false);

                // Assert - common stuff
                Assert.IsNotNull(result);
                Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
                Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count);

                // Assert - orchestration
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
                Received.InOrder(async () =>
                {
                    await _authCodeRequestComponentOverride
                        .FetchAuthCodeAndPkceVerifierAsync(default)
                        .ConfigureAwait(false);
                    await _brokerExchangeComponentOverride
                        .FetchTokensAsync(default)
                        .ConfigureAwait(false);
                });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates

                await _authCodeExchangeComponentOverride
                       .DidNotReceiveWithAnyArgs()
                       .FetchTokensAsync(default)
                       .ConfigureAwait(false);
            }
        }

        private static void MockInstanceDiscovery(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
        }
    }
}
