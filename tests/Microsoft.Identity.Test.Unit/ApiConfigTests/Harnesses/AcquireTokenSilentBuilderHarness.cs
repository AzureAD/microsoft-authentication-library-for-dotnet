// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Executors;
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
