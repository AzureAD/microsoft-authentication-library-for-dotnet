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
    internal class AcquireTokenByIntegratedWindowsAuthBuilderHarness : AbstractBuilderHarness
    {
        public AcquireTokenByIntegratedWindowsAuthParameters IntegratedWindowsAuthParametersReceived { get; private set; }
        public IPublicClientApplication ClientApplication { get; private set; }

        public async Task SetupAsync()
        {
            ClientApplication = Substitute.For<IPublicClientApplication, IPublicClientApplicationExecutor>();

            await ((IPublicClientApplicationExecutor)ClientApplication).ExecuteAsync(
                Arg.Do<AcquireTokenCommonParameters>(parameters => CommonParametersReceived = parameters),
                Arg.Do<AcquireTokenByIntegratedWindowsAuthParameters>(parameters => IntegratedWindowsAuthParametersReceived = parameters),
                CancellationToken.None).ConfigureAwait(false);
        }

        public void ValidateInteractiveParameters(string expectedUsername = null)
        {
            Assert.IsNotNull(IntegratedWindowsAuthParametersReceived);
            Assert.AreEqual(expectedUsername, IntegratedWindowsAuthParametersReceived.Username);
        }
    }
}
