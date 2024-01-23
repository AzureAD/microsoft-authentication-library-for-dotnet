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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestWithCustomWebUiTests : TestBase
    {
        private const string ExpectedRedirectUri = "https://theredirecturi";

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
        }

        private async Task ExecuteTestAsync(
            bool withTokenRequest,
            Action<ICustomWebUi> customizeWebUiBehavior,
            Func<InteractiveRequest, Task> executionBehavior)
        {
            var customWebUi = Substitute.For<ICustomWebUi>();
            customizeWebUiBehavior(customWebUi);

            using (var harness = CreateTestHarness())
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
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope, 
                    new TokenCache(harness.ServiceBundle, false));
                parameters.RedirectUri = new Uri(ExpectedRedirectUri);
                parameters.LoginHint = TestConstants.DisplayableId;
                var interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.ForceLogin,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                    CustomWebUi = customWebUi
                };

                var ui = new CustomWebUiHandler(customWebUi);
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, ui);

                var request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);

                await executionBehavior(request).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task TestInteractiveWithCustomWebUi_HappyPath_CorrectRedirectUri_CorrectQueryDataForCodeAsync()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            await ExecuteTestAsync(
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
                async request =>
                {
                    await request.RunAsync(CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestInteractiveWithCustomWebUi_IncorrectRedirectUriAsync()
        {
            await ExecuteTestAsync(
                false,
                ui => ui.AcquireAuthorizationCodeAsync(null, null, CancellationToken.None)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri("http://blech"))),
                        async request =>
                        {
                            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                                () => request.RunAsync(CancellationToken.None)).ConfigureAwait(false);
                            Assert.AreEqual(MsalError.CustomWebUiReturnedInvalidUri, ex.ErrorCode);
                        }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestInteractiveWithCustomWebUi_UnhandledExceptionAsync()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            await ExecuteTestAsync(
                false,
                 ui => ui.AcquireAuthorizationCodeAsync(null, null, CancellationToken.None)
                        .ReturnsForAnyArgs<Uri>(_ => { throw new InvalidOperationException(); }),
                async request =>
                {
                    await AssertException.TaskThrowsAsync<InvalidOperationException>(
                        () => request.RunAsync(CancellationToken.None)).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestInteractiveWithCustomWebUi_CorrectRedirectUri_NoQueryDataForCodeAsync()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            await ExecuteTestAsync(
                false,
                ui => ui.AcquireAuthorizationCodeAsync(null, null, CancellationToken.None)
                        .ReturnsForAnyArgs(Task.FromResult(new Uri(ExpectedRedirectUri))),
                async request =>
                {
                    var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                        () => request.RunAsync(CancellationToken.None)).ConfigureAwait(false);
                    Assert.AreEqual(MsalError.CustomWebUiReturnedInvalidUri, ex.ErrorCode);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestInteractiveWithCustomWebUi_IncorrectStateAsync()
        {
            // The CustomWebUi is only going to return the Uri value here with no additional data to parse to get the code, so we'll expect to fail.
            await ExecuteTestAsync(
                false,
                ui =>
                {
                    ui.AcquireAuthorizationCodeAsync(Arg.Any<Uri>(), Arg.Any<Uri>(), CancellationToken.None)
                     .Returns(Task.FromResult(new Uri(ExpectedRedirectUri + "?code=some-code&state=bad_state")));
                },
                async request =>
                {
                    var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                      () => request.RunAsync(CancellationToken.None)).ConfigureAwait(false);
                    Assert.AreEqual(MsalError.StateMismatchError, ex.ErrorCode);
                }).ConfigureAwait(false);
        }
    }
}
