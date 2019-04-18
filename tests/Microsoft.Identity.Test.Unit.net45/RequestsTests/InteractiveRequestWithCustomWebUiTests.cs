// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
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
            TestCommon.ResetInternalStaticCaches();
        }

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
        }

        private static void ExecuteTest(
            bool withTokenRequest,
            Action<ICustomWebUi> customizeWebUiBehavior,
            Action<InteractiveRequest> executionBehavior)
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
        public void TestInteractiveWithCustomWebUi_HappyPath_CorrectRedirectUri_CorrectQueryDataForCode()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            ExecuteTest(
                true,
                ui =>
                {
                    ui.AcquireAuthorizationCodeAsync(Arg.Any<Uri>(), Arg.Any<Uri>(), CancellationToken.None)
                     .Returns(x =>
                     {
                         // Capture the state from the authorizationUri and add it to the result uri
                         var authorizationUri = x[0] as Uri;
                         var state = CoreHelpers.ParseKeyValueList(authorizationUri.Query, '&', true, false, null)["state"];
                         Assert.IsTrue(!string.IsNullOrEmpty(state));

                         return Task.FromResult(new Uri(ExpectedRedirectUri + "?code=some-code&state=" + state));
                     });
                },
                request =>
                {
                    request.ExecuteAsync(CancellationToken.None)
                           .Wait();
                });
        }

        [TestMethod]
        public void TestInteractiveWithCustomWebUi_IncorrectRedirectUriAsync()
        {
            ExecuteTest(
                false,
                ui => ui.AcquireAuthorizationCodeAsync(null, null, CancellationToken.None)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri("http://blech"))),
                        async request =>
                        {
                            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() => request.ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
                            Assert.AreEqual(MsalError.CustomWebUiReturnedInvalidUri, ex.ErrorCode);
                        });
        }


        [TestMethod]
        public void TestInteractiveWithCustomWebUi_UnhandledException()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            ExecuteTest(
                false,
                 ui => ui.AcquireAuthorizationCodeAsync(null, null, CancellationToken.None)
                        .ReturnsForAnyArgs<Uri>(x => { throw new InvalidOperationException(); }),
                async request =>
                {
                    await AssertException.TaskThrowsAsync<InvalidOperationException>(() => request.ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
                });
        }

        [TestMethod]
        public void TestInteractiveWithCustomWebUi_CorrectRedirectUri_NoQueryDataForCode()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            ExecuteTest(
                false,
                ui => ui.AcquireAuthorizationCodeAsync(null, null, CancellationToken.None)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri(ExpectedRedirectUri))),
                async request =>
                {
                    var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                        () => request.ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
                    Assert.AreEqual(MsalError.CustomWebUiReturnedInvalidUri, ex.ErrorCode);
                });
        }



        [TestMethod]
        public void TestInteractiveWithCustomWebUi_IncorrectState()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            ExecuteTest(
                false,
                ui =>
                {
                    ui.AcquireAuthorizationCodeAsync(Arg.Any<Uri>(), Arg.Any<Uri>(), CancellationToken.None)
                     .Returns(Task.FromResult(new Uri(ExpectedRedirectUri + "?code=some-code&state=bad_state")));
                },
                async request =>
                {
                    var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                      () => request.ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
                    Assert.AreEqual(MsalError.StateMismatchError, ex.ErrorCode);
                });
        }
    }
}
