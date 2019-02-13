// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Identity.Test.Unit.RequestsTests.InteractiveRequestTests;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class BrokerRequestTests
    {
        BrokerInteractiveRequest _brokerInteractiveRequest;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

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


        private void ValidateBrokerResponse(MsalTokenResponse msalTokenResponse, OAuthClientValidationHandler validationHandler)
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
            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
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

                _brokerInteractiveRequest = new BrokerInteractiveRequest(parameters, interactiveParameters, harness.ServiceBundle, null);
            }
        }
    }
}
