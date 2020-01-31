// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class BrokerRequestTests : TestBase
    {
        private BrokerInteractiveRequest _brokerInteractiveRequest;
        private BrokerSilentRequest _brokerSilentRequest;

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
                    TestConstants.s_extraQueryParams);

                // Act
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                _brokerInteractiveRequest =
                    new BrokerInteractiveRequest(
                        harness.ServiceBundle,
                        parameters,
                        new AcquireTokenInteractiveParameters(),
                        null,
                        broker);
                Assert.AreEqual(false, _brokerInteractiveRequest.Broker.CanInvokeBroker());
                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(() => _brokerInteractiveRequest.Broker.AcquireTokenUsingBrokerAsync(new Dictionary<string, string>())).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void BrokerSilentRequestTest()
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
                    TestConstants.s_extraQueryParams);

                // Act
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                _brokerSilentRequest =
                    new BrokerSilentRequest(
                        parameters,
                        null,
                        harness.ServiceBundle,
                        broker);
                Assert.AreEqual(false, _brokerSilentRequest.Broker.CanInvokeBroker());
                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(() => _brokerSilentRequest.Broker.AcquireTokenUsingBrokerAsync(new Dictionary<string, string>())).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Description(".WithBroker(true), but broker not installed on the device. Device auth required, but not work place joined")]
        public void WithBrokerTrueButNotInstalled_DeviceAuthRequired_BrokerInstallInvoked()
        {

        }


        private void ValidateBrokerResponse(MsalTokenResponse msalTokenResponse, Action<Exception> validationHandler)
        {
            try
            {
                //Testing interactive respose
                _brokerInteractiveRequest.ValidateResponseFromBroker(msalTokenResponse);

                Assert.Fail("MsalServiceException should have been thrown here");
            }
            catch (MsalServiceException exc)
            {
                try
                {
                    //Testing silent respose
                    _brokerSilentRequest.ValidateResponseFromBroker(msalTokenResponse);

                    Assert.Fail("MsalServiceException should have been thrown here");
                }
                catch (MsalServiceException exc2)
                {
                    validationHandler(exc2);
                }
                validationHandler(exc);
            }

        }

        private void CreateBrokerHelper()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    extraQueryParameters: TestConstants.s_extraQueryParams,
                    claims: TestConstants.Claims);

                parameters.IsBrokerEnabled = true;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters();
                AcquireTokenSilentParameters acquireTokenSilentParameters = new AcquireTokenSilentParameters();

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new MockWebUI());

                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                _brokerInteractiveRequest =
                    new BrokerInteractiveRequest(
                        harness.ServiceBundle,
                        parameters,
                        interactiveParameters,
                        null,
                        broker);

                _brokerSilentRequest =
                    new BrokerSilentRequest(
                        parameters,
                        acquireTokenSilentParameters,
                        harness.ServiceBundle,
                        broker);
            }
        }
    }
}
