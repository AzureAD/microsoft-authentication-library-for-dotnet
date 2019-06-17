using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class InstanceDiscoveryManagerTests : TestBase
    {
        [TestMethod]
        public async Task B2C_GetMetadataAsync()
        {
            await ValidateSelfEntryAsync(new Uri(MsalTestConstants.B2CAuthority))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ADFS_GetMetadataAsync()
        {
            await ValidateSelfEntryAsync(new Uri(MsalTestConstants.ADFSAuthority))
                .ConfigureAwait(false);
        }

        private async Task ValidateSelfEntryAsync(Uri authority)
        {
            using (var harness = CreateTestHarness())
            {
                var entry = await harness.ServiceBundle.InstanceDiscoveryManager
                    .GetMetadataEntryAsync(
                        authority,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .ConfigureAwait(false);

                Assert.AreEqual(authority.Host, entry.PreferredCache);
                Assert.AreEqual(authority.Host, entry.PreferredNetwork);
                Assert.AreEqual(authority.Host, entry.Aliases.Single());
            }
        }
    }
}
