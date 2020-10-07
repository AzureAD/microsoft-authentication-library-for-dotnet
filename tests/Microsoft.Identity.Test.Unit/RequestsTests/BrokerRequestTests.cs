// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using NSubstitute.ExceptionExtensions;
using Microsoft.Identity.Client.Internal.Requests.Silent;
using Microsoft.Identity.Client.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.Identity.Client.AuthScheme;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class BrokerRequestTests : TestBase
    {
        private BrokerInteractiveRequestComponent _brokerInteractiveRequest;
        private SilentBrokerAuthStrategy _brokerSilentAuthStrategy;
        private AuthenticationRequestParameters _parameters;
        private AcquireTokenSilentParameters _acquireTokenSilentParameters;
        private HttpResponse _brokerHttpResponse;

        [TestMethod]
        public void BrokerResponseTest()
        {
            // Arrange
            CreateBrokerHelper();

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = TestConstants.s_scope.AsSingleString(),
                TokenType = "Bearer"
            };

            // Act
            _brokerInteractiveRequest.ValidateResponseFromBroker(response);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("access-token", response.AccessToken);
            Assert.AreEqual(MockHelpers.CreateClientInfo(), response.ClientInfo);
        }

        [TestMethod]
        public void BrokerErrorResponseTest()
        {
            CreateBrokerHelper();

            var response = new MsalTokenResponse
            {
                Error = "MSALErrorDomain",
                ErrorDescription = "error_description: Server returned less scopes than requested"
            };

            ValidateBrokerResponse(
                response,
                exception =>
                {
                    var exc = exception as MsalServiceException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(response.Error, exc.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.BrokerResponseError + response.ErrorDescription, exc.Message);
                });
        }

        [TestMethod]
        public void BrokerInteractionRequiredErrorResponseTest()
        {
            CreateBrokerHelper();

            var response = new MsalTokenResponse
            {
                Error = MsalError.InteractionRequired,
                ErrorDescription = MsalError.InteractionRequired,
                HttpResponse = _brokerHttpResponse
            };

            ValidateBrokerResponse(
                response,
                exception =>
                {
                    var exc = exception as MsalUiRequiredException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.InteractionRequired, exc.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.BrokerResponseError + MsalError.InteractionRequired, exc.Message);
                    Assert.AreEqual(exc.StatusCode, (int)HttpStatusCode.Unauthorized);
                    Assert.AreEqual(exc.ResponseBody, "SomeBody");
                    Assert.IsNotNull(exc.Headers);
                });
        }

        [TestMethod]
        public void BrokerInvalidGrantErrorResponseTest()
        {
            CreateBrokerHelper();

            var response = new MsalTokenResponse
            {
                Error = MsalError.InvalidGrantError,
                ErrorDescription = MsalError.InvalidGrantError,
                HttpResponse = _brokerHttpResponse
            };

            ValidateBrokerResponse(
                response,
                exception =>
                {
                    var exc = exception as MsalUiRequiredException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.InvalidGrantError, exc.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.BrokerResponseError + MsalError.InvalidGrantError, exc.Message);
                    Assert.AreEqual(exc.StatusCode, (int)HttpStatusCode.Unauthorized);
                    Assert.AreEqual(exc.ResponseBody, "SomeBody");
                    Assert.IsNotNull(exc.Headers);
                });
        }

        [TestMethod]
        public void BrokerUnknownErrorResponseTest()
        {
            CreateBrokerHelper();

            var response = new MsalTokenResponse
            {
                Error = null,
                ErrorDescription = null
            };

            ValidateBrokerResponse(
                response,
                exception =>
                {
                    var exc = exception as MsalServiceException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.BrokerResponseReturnedError, exc.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.BrokerResponseReturnedError, exc.Message);
                });
        }

        [TestMethod]
        public void BrokerInteractiveRequestTest()
        {
            string CanonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(TestConstants.AuthorityTestTenant));

            using (var harness = CreateTestHarness())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    null,
                    TestConstants.ExtraQueryParameters);

                // Act
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                _brokerInteractiveRequest =
                    new BrokerInteractiveRequestComponent(
                        parameters,
                        null,
                        broker,
                        "install_url");
                Assert.AreEqual(false, _brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable());

                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(
                    () => _brokerInteractiveRequest.Broker.AcquireTokenInteractiveAsync(
                        parameters, new AcquireTokenInteractiveParameters())).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void BrokerSilentRequestTest()
        {
            string CanonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(TestConstants.AuthorityTestTenant));

            using (var harness = CreateBrokerHelper())
            {
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                _brokerSilentAuthStrategy =
                    new SilentBrokerAuthStrategy(
                        new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters),
                        harness.ServiceBundle,
                        _parameters,
                        _acquireTokenSilentParameters,
                        broker);

                Assert.AreEqual(false, _brokerSilentAuthStrategy.Broker.IsBrokerInstalledAndInvokable());
                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(() => _brokerSilentAuthStrategy.Broker.AcquireTokenSilentAsync(_parameters, _acquireTokenSilentParameters)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void BrokerGetAccountsAsyncOnUnsupportedPlatformTest()
        {
            using (var harness = CreateTestHarness())
            {
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);

                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(
                    () => broker.GetAccountsAsync(TestConstants.ClientId, TestConstants.RedirectUri))
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void BrokerRemoveAccountAsyncOnUnsupportedPlatformTest()
        {
            using (var harness = CreateTestHarness())
            {
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);

                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(() => broker.RemoveAccountAsync(TestConstants.ClientId, new Account("test", "test", "test"))).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task BrokerGetAccountsWithBrokerInstalledTestAsync()
        {
            // Arrange
            var mockBroker = Substitute.For<IBroker>();
            var expectedAccount = Substitute.For<IAccount>();
            mockBroker.GetAccountsAsync(TestConstants.ClientId, TestConstants.RedirectUri).Returns(new[] { expectedAccount });
            mockBroker.IsBrokerInstalledAndInvokable().Returns(true);

            var platformProxy = Substitute.For<IPlatformProxy>();
            platformProxy.CanBrokerSupportSilentAuth().Returns(true);
            platformProxy.CreateBroker(null).Returns(mockBroker);


            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithBroker(true)
                .WithPlatformProxy(platformProxy)
                .Build();

            // Act
            var actualAccount = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Assert that MSAL acquires an account from the broker cache
            Assert.AreSame(expectedAccount, actualAccount.Single());
        }

        [TestMethod]
        public async Task BrokerGetAccountsWithBrokerNotInstalledTestAsync()
        {
            // Arrange
            var mockBroker = Substitute.For<IBroker>();
            var expectedAccount = Substitute.For<IAccount>();
            mockBroker.GetAccountsAsync(TestConstants.ClientId, TestConstants.RedirectUri).Returns(new[] { expectedAccount });
            mockBroker.IsBrokerInstalledAndInvokable().Returns(false);

            var platformProxy = Substitute.For<IPlatformProxy>();
            platformProxy.CanBrokerSupportSilentAuth().Returns(true);
            platformProxy.CreateBroker(null).Returns(mockBroker);

            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithBroker(true)
                .WithPlatformProxy(platformProxy)
                .Build();

            // Act
            var actualAccount = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Assert that MSAL attempts to acquire an account locally when broker is not available
            Assert.IsTrue(actualAccount.Count() == 1);
        }

        [TestMethod]
        public void SilentAuthStrategyFallbackTest()
        {
            using (var harness = CreateBrokerHelper())
            {
                //SilentRequest should always get an exception from the local client strategy and use the broker strategy instead when the right error codes
                //are thrown.

                // Arrange
                var mockBroker = Substitute.For<IBroker>();
                var expectedAccount = Substitute.For<IAccount>();
                mockBroker.GetAccountsAsync(TestConstants.ClientId, TestConstants.RedirectUri).Returns(new[] { expectedAccount });
                mockBroker.IsBrokerInstalledAndInvokable().Returns(false);

                var platformProxy = Substitute.For<IPlatformProxy>();
                platformProxy.CanBrokerSupportSilentAuth().Returns(true);
                platformProxy.CreateBroker(null).Returns(mockBroker);

                harness.ServiceBundle.SetPlatformProxyForTest(platformProxy);
                
                var mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                var mockBrokerStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                var mockBrokerAuthenticationResult = new AuthenticationResult();

                var invlidGrantException = new MsalClientException(MsalError.InvalidGrantError);
                var NoAccountException = new MsalClientException(MsalError.NoAccountForLoginHint);
                var NoTokensException = new MsalClientException(MsalError.NoTokensFoundError);

                mockBrokerStrategy.ExecuteAsync(new CancellationToken()).Returns(mockBrokerAuthenticationResult);
                mockClientStrategy.ExecuteAsync(new CancellationToken()).Throws(invlidGrantException);

                //Execute silent request with invalid grant
                var silentRequest = new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters, mockClientStrategy, mockBrokerStrategy);
                var result = silentRequest.ExecuteTestAsync(new CancellationToken()).Result;
                Assert.AreEqual(result, mockBrokerAuthenticationResult);

                //Execute silent request with no accounts exception
                mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                mockClientStrategy.ExecuteAsync(new CancellationToken()).Throws(NoAccountException);
                silentRequest = new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters, mockClientStrategy, mockBrokerStrategy);
                result = silentRequest.ExecuteTestAsync(new CancellationToken()).Result;
                Assert.AreEqual(result, mockBrokerAuthenticationResult);

                //Execute silent request with no tokens exception
                mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                mockClientStrategy.ExecuteAsync(new CancellationToken()).Throws(NoTokensException);
                silentRequest = new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters, mockClientStrategy, mockBrokerStrategy);
                result = silentRequest.ExecuteTestAsync(new CancellationToken()).Result;
                Assert.AreEqual(result, mockBrokerAuthenticationResult);
            }
        }

        [TestMethod]
        public void NoTokenFoundThrowsUIRequiredTest()
        {
            using (var harness = CreateBrokerHelper())
            {

                var response = new MsalTokenResponse
                {
                    Scope = TestConstants.s_scope.AsSingleString(),
                    TokenType = "Bearer",
                    Error = BrokerResponseConst.NoTokenFound
                };

                try
                {
                    _brokerSilentAuthStrategy.ValidateResponseFromBroker(response);
                }
                catch(MsalUiRequiredException ex)
                {
                    Assert.IsTrue(ex.ErrorCode == BrokerResponseConst.NoTokenFound);
                    return;
                }

                Assert.Fail("Wrong Exception thrown.");
            }
        }

        private void ValidateBrokerResponse(MsalTokenResponse msalTokenResponse, Action<Exception> validationHandler)
        {
            try
            {
                //Testing interactive response
                _brokerInteractiveRequest.ValidateResponseFromBroker(msalTokenResponse);

                Assert.Fail("MsalServiceException should have been thrown here");
            }
            catch (MsalServiceException exc)
            {
                try
                {
                    //Testing silent response
                    _brokerSilentAuthStrategy.ValidateResponseFromBroker(msalTokenResponse);

                    Assert.Fail("MsalServiceException should have been thrown here");
                }
                catch (MsalServiceException exc2)
                {
                    validationHandler(exc2);
                }
                validationHandler(exc);
            }

        }

        private MockHttpAndServiceBundle CreateBrokerHelper()
        {
            MockHttpAndServiceBundle harness = CreateTestHarness();
            
                _parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    extraQueryParameters: TestConstants.ExtraQueryParameters,
                    claims: TestConstants.Claims);

                _parameters.IsBrokerConfigured = true;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters();
                _acquireTokenSilentParameters = new AcquireTokenSilentParameters();

                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                _brokerInteractiveRequest =
                    new BrokerInteractiveRequestComponent(
                        _parameters,
                        interactiveParameters,
                        broker,
                        "install_url");

                _brokerSilentAuthStrategy =
                    new SilentBrokerAuthStrategy(
                        new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters),
                        harness.ServiceBundle,
                        _parameters,
                        _acquireTokenSilentParameters,
                        broker);

            _brokerHttpResponse = new HttpResponse();
            _brokerHttpResponse.Body = "SomeBody";
            _brokerHttpResponse.StatusCode = HttpStatusCode.Unauthorized;
            _brokerHttpResponse.Headers = new HttpResponseMessage().Headers;

            return harness;
        }
    }
}
