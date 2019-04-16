// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests.Harnesses
{
    internal class GetAuthorizationRequestUrlBuilderHarness : AbstractBuilderHarness
    {
        public GetAuthorizationRequestUrlParameters AuthorizationRequestUrlParametersReceived { get; private set; }
        public IConfidentialClientApplication ClientApplication { get; private set; }

        public async Task SetupAsync()
        {
            ClientApplication = Substitute.For<IConfidentialClientApplication, IConfidentialClientApplicationExecutor>();

            await ((IConfidentialClientApplicationExecutor)ClientApplication).ExecuteAsync(
                Arg.Do<AcquireTokenCommonParameters>(parameters => CommonParametersReceived = parameters),
                Arg.Do<GetAuthorizationRequestUrlParameters>(parameters => AuthorizationRequestUrlParametersReceived = parameters),
                CancellationToken.None).ConfigureAwait(false);
        }

        public void ValidateInteractiveParameters(
            IAccount expectedAccount = null,
            IEnumerable<string> expectedExtraScopesToConsent = null,
            string expectedLoginHint = null,
            string expectedPromptValue = null)
        {
            Assert.IsNotNull(AuthorizationRequestUrlParametersReceived);
            Assert.AreEqual(expectedAccount, AuthorizationRequestUrlParametersReceived.Account);
            CoreAssert.AreScopesEqual(
                (expectedExtraScopesToConsent ?? new List<string>()).AsSingleString(),
                AuthorizationRequestUrlParametersReceived.ExtraScopesToConsent.AsSingleString());
            Assert.AreEqual(expectedLoginHint, AuthorizationRequestUrlParametersReceived.LoginHint);

            var interactiveParameters = AuthorizationRequestUrlParametersReceived.ToInteractiveParameters();

            Assert.IsNotNull(interactiveParameters);

            Assert.AreEqual(expectedAccount, interactiveParameters.Account);
            CoreAssert.AreScopesEqual(
                (expectedExtraScopesToConsent ?? new List<string>()).AsSingleString(),
                interactiveParameters.ExtraScopesToConsent.AsSingleString());
            Assert.AreEqual(expectedLoginHint, interactiveParameters.LoginHint);
            Assert.AreEqual(expectedPromptValue, interactiveParameters.Prompt.PromptValue);
            Assert.IsNotNull(interactiveParameters.UiParent);
            Assert.AreEqual(false, interactiveParameters.UseEmbeddedWebView);
        }
    }
}
