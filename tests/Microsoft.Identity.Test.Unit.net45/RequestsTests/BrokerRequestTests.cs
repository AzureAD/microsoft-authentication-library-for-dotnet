// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
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

        [TestMethod]
        [TestCategory("BrokerRequestTests")]
        public void BrokerResponseTest()
        {
            // Arrange
            CreateBrokerHelper();

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
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
        [TestCategory("BrokerRequestTests")]
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
        [TestCategory("BrokerRequestTests")]
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
        [TestCategory("BrokerRequestTests")]
        public void BrokerInteractiveRequestTest()
        {
            string CanonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(MsalTestConstants.AuthorityTestTenant));

            using (var harness = CreateTestHarness())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityTestTenant,
                    null,
                    null,
                    null,
                    MsalTestConstants.ExtraQueryParams);

                // Act
                BrokerFactory brokerFactory = new BrokerFactory();
                BrokerInteractiveRequest brokerInteractiveRequest = new BrokerInteractiveRequest(parameters, null, null, null, brokerFactory.Create(harness.ServiceBundle));
                Assert.AreEqual(false, brokerInteractiveRequest._broker.CanInvokeBroker(null));
                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(() => brokerInteractiveRequest._broker.AcquireTokenUsingBrokerAsync(new Dictionary<string, string>())).ConfigureAwait(false);
            }
        }

        private void ValidateBrokerResponse(MsalTokenResponse msalTokenResponse, Action<Exception> validationHandler)
        {
            try
            {
                _brokerInteractiveRequest.ValidateResponseFromBroker(msalTokenResponse);

                Assert.Fail("MsalServiceException should have been thrown here");
            }
            catch (MsalServiceException exc)
            {
                validationHandler(exc);
            }

        }

        private void CreateBrokerHelper()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    new TokenCache(harness.ServiceBundle),
                    extraQueryParameters: MsalTestConstants.ExtraQueryParams,
                    claims: MsalTestConstants.Claims);

                parameters.IsBrokerEnabled = true;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters();

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new MockWebUI());

                BrokerFactory brokerFactory = new BrokerFactory();
                _brokerInteractiveRequest = new BrokerInteractiveRequest(parameters, interactiveParameters, harness.ServiceBundle, null, brokerFactory.Create(harness.ServiceBundle));
            }
        }
    }
}
