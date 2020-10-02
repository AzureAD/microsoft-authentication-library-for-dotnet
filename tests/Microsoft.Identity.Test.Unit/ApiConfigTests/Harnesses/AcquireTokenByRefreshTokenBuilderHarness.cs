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
    internal class AcquireTokenByRefreshTokenBuilderHarness : AbstractBuilderHarness
    {
        public AcquireTokenByRefreshTokenParameters RefreshTokenParametersReceived { get; private set; }
        public IConfidentialClientApplication ClientApplication { get; private set; }

        public async Task SetupAsync()
        {
            ClientApplication = Substitute.For<IConfidentialClientApplication, IClientApplicationBaseExecutor>();

            await ((IClientApplicationBaseExecutor)ClientApplication).ExecuteAsync(
                Arg.Do<AcquireTokenCommonParameters>(parameters => CommonParametersReceived = parameters),
                Arg.Do<AcquireTokenByRefreshTokenParameters>(parameters => RefreshTokenParametersReceived = parameters),
                CancellationToken.None).ConfigureAwait(false);
        }

        public void ValidateInteractiveParameters(string expectedRefreshToken = null)
        {
            Assert.IsNotNull(RefreshTokenParametersReceived);
            Assert.AreEqual(expectedRefreshToken, RefreshTokenParametersReceived.RefreshToken);
        }
    }
}
