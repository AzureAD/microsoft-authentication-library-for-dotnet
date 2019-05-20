// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests.Harnesses
{
    internal class AcquireTokenInteractiveBuilderHarness : AbstractBuilderHarness
    {
        public AcquireTokenInteractiveParameters InteractiveParametersReceived { get; private set; }
        public IPublicClientApplication ClientApplication { get; private set; }

        public IPublicClientApplicationExecutor Executor => (IPublicClientApplicationExecutor)ClientApplication;

        public async Task SetupAsync()
        {
            ClientApplication = Substitute.For<IPublicClientApplication, IPublicClientApplicationExecutor>();

            await ((IPublicClientApplicationExecutor)ClientApplication).ExecuteAsync(
                Arg.Do<AcquireTokenCommonParameters>(
                    parameters =>
                        CommonParametersReceived = parameters),
                Arg.Do<AcquireTokenInteractiveParameters>(
                    parameters =>
                        InteractiveParametersReceived = parameters),
                CancellationToken.None).ConfigureAwait(false);
        }


        public void ValidateInteractiveParameters(
           IAccount expectedAccount = null,
           IEnumerable<string> expectedExtraScopesToConsent = null,
           string expectedLoginHint = null,
           string expectedPromptValue = null,
           ICustomWebUi expectedCustomWebUi = null)
        {
             ValidateInteractiveParameters(
                Maybe<bool>.Empty(),
                expectedAccount,
                expectedExtraScopesToConsent,
                expectedLoginHint,
                expectedPromptValue,
                expectedCustomWebUi);
        }

        public void ValidateInteractiveParameters(
            Maybe<bool> expectedEmbeddedWebView,
            IAccount expectedAccount = null,
            IEnumerable<string> expectedExtraScopesToConsent = null,
            string expectedLoginHint = null,
            string expectedPromptValue = null,
            ICustomWebUi expectedCustomWebUi = null)
        {
            Assert.IsNotNull(InteractiveParametersReceived);

            Assert.AreEqual(expectedAccount, InteractiveParametersReceived.Account);
            CoreAssert.AreScopesEqual(
                (expectedExtraScopesToConsent ?? new List<string>()).AsSingleString(),
                InteractiveParametersReceived.ExtraScopesToConsent.AsSingleString());
            Assert.AreEqual(expectedLoginHint, InteractiveParametersReceived.LoginHint);
            Assert.AreEqual(expectedPromptValue ?? Prompt.SelectAccount.PromptValue, InteractiveParametersReceived.Prompt.PromptValue);
            Assert.IsNotNull(InteractiveParametersReceived.UiParent);
            Assert.AreEqual(expectedEmbeddedWebView, InteractiveParametersReceived.UseEmbeddedWebView);
            Assert.AreEqual(expectedCustomWebUi, InteractiveParametersReceived.CustomWebUi);
        }
    }
}
