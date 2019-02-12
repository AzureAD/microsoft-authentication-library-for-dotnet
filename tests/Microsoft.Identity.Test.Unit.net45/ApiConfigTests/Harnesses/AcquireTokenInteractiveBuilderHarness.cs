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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
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
            bool expectedEmbeddedWebView = false,
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