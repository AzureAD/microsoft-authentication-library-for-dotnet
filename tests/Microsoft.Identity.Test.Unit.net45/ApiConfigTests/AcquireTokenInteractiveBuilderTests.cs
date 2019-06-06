// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Unit.ApiConfigTests.Harnesses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    [TestCategory("BuilderTests")]
    public class AcquireTokenInteractiveBuilderTests
    {
        private AcquireTokenInteractiveBuilderHarness _harness;

        [TestInitialize]
        public async Task TestInitializeAsync()
        {
            TestCommon.ResetInternalStaticCaches();
            _harness = new AcquireTokenInteractiveBuilderHarness();
            await _harness.SetupAsync()
                          .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderAsync()
        {
            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters();
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderWithAccountAsync()
        {
            var account = Substitute.For<IAccount>();
            account.Username.Returns(MsalTestConstants.DisplayableId);

            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithAccount(account)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(account, expectedLoginHint: MsalTestConstants.DisplayableId);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderWithLoginHintAsync()
        {
            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithLoginHint(MsalTestConstants.DisplayableId)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(expectedLoginHint: MsalTestConstants.DisplayableId);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderWithAccountAndLoginHintAsync()
        {
            var account = Substitute.For<IAccount>();
            account.Username.Returns(MsalTestConstants.DisplayableId);

            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithAccount(account)
                                                         .WithLoginHint("SomeOtherLoginHint")
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(account, expectedLoginHint: "SomeOtherLoginHint");
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderWithPromptAndExtraQueryParametersAsync()
        {
            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithLoginHint(MsalTestConstants.DisplayableId)
                                                         .WithExtraQueryParameters("domain_hint=mydomain.com")
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(
                ApiEvent.ApiIds.AcquireTokenInteractive,
                expectedExtraQueryParameters: new Dictionary<string, string> { { "domain_hint", "mydomain.com" } });
            _harness.ValidateInteractiveParameters(
                expectedLoginHint: MsalTestConstants.DisplayableId);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderWithCustomWebUiAsync()
        {
            var customWebUi = Substitute.For<ICustomWebUi>();

            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithCustomWebUi(customWebUi)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);
            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(expectedCustomWebUi: customWebUi);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractive_SystemWebview_Async()
        {
            var customWebUi = Substitute.For<ICustomWebUi>();

            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithUseEmbeddedWebView(false)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(expectedEmbeddedWebView: WebViewPreference.System);
        }

#if DESKTOP
        [TestMethod]
        public async Task TestAcquireTokenInteractive_Embedded_Async()
        {
            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithUseEmbeddedWebView(true)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);
            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(expectedEmbeddedWebView: WebViewPreference.Embedded);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractive_EmbeddedAndSystemOptions_Async()
        {
            var options = new SystemWebViewOptions();
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>

                 AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithSystemWebViewOptions(options)
                                                         .WithUseEmbeddedWebView(true)
                                                         .ExecuteAsync()
                                                        ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.SystemWebviewOptionsNotApplicable, ex.ErrorCode);
        }
#endif

#if NET_CORE
        [TestMethod]
        public async Task TestAcquireTokenInteractive_EmbeddedNetCore_Async()
        {
            var customWebUi = Substitute.For<ICustomWebUi>();

            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                 AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithUseEmbeddedWebView(true)
                                                         .ExecuteAsync()
                                                        ).ConfigureAwait(false);
            Assert.AreEqual(MsalError.WebviewUnavailable, ex.ErrorCode);
        }
#endif

        [TestMethod]
        public async Task TestAcquireTokenInteractive_Options_Async()
        {
            var options = new SystemWebViewOptions();
            await AcquireTokenInteractiveParameterBuilder.Create(_harness.Executor, MsalTestConstants.Scope)
                                                         .WithSystemWebViewOptions(options)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenInteractive);
            _harness.ValidateInteractiveParameters(
                expectedEmbeddedWebView: WebViewPreference.System, // If system webview options are set, force usage of system webview
                browserOptions: options);
        }
    }
}
