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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests.Harnesses
{
    internal class AcquireTokenSilentBuilderHarness : AbstractBuilderHarness
    {
        public AcquireTokenSilentParameters SilentParametersReceived { get; private set; }
        public IClientApplicationBase ClientApplication { get; private set; }

        public async Task SetupAsync()
        {
            ClientApplication = Substitute.For<IClientApplicationBase, IClientApplicationBaseExecutor>();

            await ((IClientApplicationBaseExecutor)ClientApplication).ExecuteAsync(
                Arg.Do<AcquireTokenCommonParameters>(parameters => CommonParametersReceived = parameters),
                Arg.Do<AcquireTokenSilentParameters>(parameters => SilentParametersReceived = parameters),
                CancellationToken.None).ConfigureAwait(false);
        }

        public void ValidateInteractiveParameters(
            IAccount expectedAccount = null,
            string expectedLoginHint = null,
            bool expectedForceRefresh = false)
        {
            Assert.IsNotNull(SilentParametersReceived);
            Assert.AreEqual(expectedAccount, SilentParametersReceived.Account);
            Assert.AreEqual(expectedForceRefresh, SilentParametersReceived.ForceRefresh);
            Assert.AreEqual(expectedLoginHint, SilentParametersReceived.LoginHint);
        }
    }
}