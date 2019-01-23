using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.TelemetryCore;
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
            _harness = new AcquireTokenOnBehalfOfBuilderHarness();
            await _harness.SetupAsync()
                          .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAcquireTokenInteractiveBuilderAsync()
        {
            await AcquireTokenByRefreshTokenParameterBuilder.Create(_harness.ClientApplication, MsalTestConstants.Scope, null)
                                                         .ExecuteAsync()
                                                         .ConfigureAwait(false);

            _harness.ValidateCommonParameters(ApiEvent.ApiIds.AcquireTokenWithScope);
            _harness.ValidateInteractiveParameters();
        }
    }
}
