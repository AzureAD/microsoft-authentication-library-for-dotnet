// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Unit.ApiConfigTests.Harnesses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    [TestCategory("BuilderTests")]
    public class AcquireTokenOnBehalfOfBuilderTests
    {
        private AcquireTokenOnBehalfOfBuilderHarness _harness;

        [TestInitialize]
        public async Task TestInitializeAsync()
        {
            TestCommon.ResetInternalStaticCaches();
            _harness = new AcquireTokenOnBehalfOfBuilderHarness();
            await _harness.SetupAsync()
                          .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAcquireTokenOnBehalfOfBuilderAsync()
        {
            await AcquireTokenOnBehalfOfParameterBuilder.Create(_harness.Executor,
                TestConstants.s_scope,
                new UserAssertion(TestConstants.UserAssertion))
                    .ExecuteAsync()
                    .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenOnBehalfOf);
            _harness.ValidateOnBehalfOfParameters(TestConstants.UserAssertion);
        }

        [TestMethod]
        public async Task TestAcquireTokenOnBehalfOfBuilder_WithSend5xC_Async()
        {
            await AcquireTokenOnBehalfOfParameterBuilder.Create(_harness.Executor,
                TestConstants.s_scope,
                new UserAssertion(TestConstants.UserAssertion))
                .WithSendX5C(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenOnBehalfOf);
            _harness.ValidateOnBehalfOfParameters(TestConstants.UserAssertion, expectedSendX5C: true);
        }

        [TestMethod]
        public async Task TestAcquireTokenOnBehalfOfBuilder_WithForceRefresh_Async()
        {
            await AcquireTokenOnBehalfOfParameterBuilder.Create(_harness.Executor,
                TestConstants.s_scope,
                new UserAssertion(TestConstants.UserAssertion))
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenOnBehalfOf);
            _harness.ValidateOnBehalfOfParameters(TestConstants.UserAssertion, forceRefresh: true);
        }
    }
}
