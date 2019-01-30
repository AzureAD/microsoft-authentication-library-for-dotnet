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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestWithCustomWebUiTests
    {
        private const string ExpectedRedirectUri = "https://theredirecturi";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
        }

        private static void ExecuteTest(bool withTokenRequest, Action<ICustomWebUi> customizeWebUiBehavior, Action<InteractiveRequest> executionBehavior)
        {
            var customWebUi = Substitute.For<ICustomWebUi>();
            customizeWebUiBehavior(customWebUi);

            using (var harness = new MockHttpAndServiceBundle())
            {
                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                if (withTokenRequest)
                {
                    harness.HttpManager.AddMockHandler(
                        new MockHttpMessageHandler
                        {
                            ExpectedMethod = HttpMethod.Post,
                            ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                        });
                }

                var parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope);
                parameters.RedirectUri = new Uri(ExpectedRedirectUri);
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                var interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.ForceLogin,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                    CustomWebUi = customWebUi
                };

                var request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new CustomWebUiHandler(customWebUi));

                executionBehavior(request);
            }
        }

        [TestMethod]
        public void TestInteractiveWithCustomWebUi_IncorrectRedirectUri()
        {
            ExecuteTest(
                false,
                ui => ui.AcquireAuthorizationCodeAsync(null, null)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri("http://blech"))),
                request =>
                {
                    try
                    {
                        request.ExecuteAsync(CancellationToken.None)
                               .Wait();
                        Assert.Fail("MsalException should have been thrown here");
                    }
                    catch (Exception exc)
                    {
                        Assert.IsTrue(exc.InnerException is MsalServiceException);
                        Assert.AreEqual(CoreErrorCodes.UnknownError, ((MsalServiceException)exc.InnerException).ErrorCode);
                    }
                });
        }

        [TestMethod]
        public void TestInteractiveWithCustomWebUi_CorrectRedirectUri_NoQueryDataForCode()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            ExecuteTest(
                false,
                ui => ui.AcquireAuthorizationCodeAsync(null, null)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri(ExpectedRedirectUri))),
                request =>
                {
                    try
                    {
                        request.ExecuteAsync(CancellationToken.None)
                               .Wait();
                        Assert.Fail("MsalException should have been thrown here");
                    }
                    catch (Exception exc)
                    {
                        Assert.IsTrue(exc.InnerException is MsalServiceException);
                        Assert.AreEqual(CoreErrorCodes.AuthenticationFailed, ((MsalServiceException)exc.InnerException).ErrorCode);
                    }
                });
        }

        [TestMethod]
        public void TestInteractiveWithCustomWebUi_CorrectRedirectUri_CorrectQueryDataForCode()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            ExecuteTest(
                true,
                ui => ui.AcquireAuthorizationCodeAsync(null, null)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri(ExpectedRedirectUri + "?code=some-code"))),
                request =>
                {
                    request.ExecuteAsync(CancellationToken.None)
                           .Wait();
                });
        }
    }
}