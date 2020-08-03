using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    [DeploymentItem("Resources\\local-imds-response.json")]
    public class RegionDiscoveryTests : TestBase
    {
        private const string Region = "centralus";

        [TestMethod]
        public async Task SuccessfulResponseFromEnvironmentVariableAsync ()
        {
            try
            {
                Environment.SetEnvironmentVariable("REGION_NAME", Region);

                var regionDiscovered = await RegionDiscovery.GetInstance.getRegionAsync().ConfigureAwait(false);

                Assert.IsNotNull(regionDiscovered);
                Assert.AreEqual(Region, regionDiscovered);
            }
            finally
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
            }
            
        }

        [TestMethod]
        public async Task SuccessfulResponseFromLocalImdsAsync ()
        {
            using (MockHttpManager mockHttpManager = new MockHttpManager())
            {
                // add mock response for local imds call
                mockHttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "http://169.254.169.254/metadata/instance/compute",
                        ExpectedRequestHeaders = new Dictionary<string, string>
                        {
                            {"api-version", "2019-06-01"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText(
                            ResourceHelper.GetTestResourceRelativePath("local-imds-response.json")))
                    });

                RegionDiscovery regionDiscovery = RegionDiscovery.GetInstance;
                regionDiscovery.setHttpManager(mockHttpManager);
                var regionDiscovered = await regionDiscovery.getRegionAsync().ConfigureAwait(false);

                Assert.IsNotNull(regionDiscovered);
                Assert.AreEqual(Region, regionDiscovered);
            }
        }

        [TestMethod]
        public async Task ErrorResponseFromLocalImdsAsync()
        {
            using (MockHttpManager mockHttpManager = new MockHttpManager())
            {
                mockHttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                         ExpectedMethod = HttpMethod.Get,
                         ExpectedUrl = "http://169.254.169.254/metadata/instance/compute",
                         ExpectedRequestHeaders = new Dictionary<string, string>
                         {
                            {"api-version", "2019-06-01"}
                         },
                         ResponseMessage = MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound)
                     });

                try
                {
                    RegionDiscovery regionDiscovery = RegionDiscovery.GetInstance;
                    regionDiscovery.setHttpManager(mockHttpManager);
                    var region = await RegionDiscovery.GetInstance.getRegionAsync().ConfigureAwait(false);
                }
                catch (MsalClientException e)
                {
                    Assert.IsNotNull(e);
                    Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
                }
            }
        }
    }
}
