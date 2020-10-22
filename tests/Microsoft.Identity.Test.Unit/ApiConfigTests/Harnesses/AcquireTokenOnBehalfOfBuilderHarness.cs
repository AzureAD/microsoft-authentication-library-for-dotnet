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
    internal class AcquireTokenOnBehalfOfBuilderHarness : AbstractBuilderHarness
    {
        public AcquireTokenOnBehalfOfParameters OnBehalfOfParametersReceived { get; private set; }
        public IConfidentialClientApplication ClientApplication { get; private set; }

        public async Task SetupAsync()
        {
            ClientApplication = Substitute.For<IConfidentialClientApplication, IConfidentialClientApplicationExecutor>();

            await ((IConfidentialClientApplicationExecutor)ClientApplication).ExecuteAsync(
                Arg.Do<AcquireTokenCommonParameters>(parameters => CommonParametersReceived = parameters),
                Arg.Do<AcquireTokenOnBehalfOfParameters>(parameters => OnBehalfOfParametersReceived = parameters),
                CancellationToken.None).ConfigureAwait(false);
        }

        public void ValidateInteractiveParameters(bool expectedSendX5C = false, UserAssertion expectedUserAssertion = null)
        {
            Assert.IsNotNull(OnBehalfOfParametersReceived);
            Assert.AreEqual(expectedSendX5C, OnBehalfOfParametersReceived.SendX5C);
            Assert.AreEqual(expectedUserAssertion, OnBehalfOfParametersReceived.UserAssertion);
        }
    }
}
