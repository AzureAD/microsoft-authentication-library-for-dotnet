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
using OpenQA.Selenium.Remote;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    [DeploymentItem("Resources\\local-imds-response.json")]
    public class RegionDiscoveryProviderTests : TestBase
    {
        private const string Region = "centralus";
        private MockHttpManager mockHttpManager = new MockHttpManager();

        [TestMethod]
        public async Task SuccessfulResponseFromEnvironmentVariableAsync ()
        {
            try
            {
                Environment.SetEnvironmentVariable("REGION_NAME", Region);

                IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(mockHttpManager);
                var regionDiscovered = await regionDiscoveryProvider.getRegionAsync().ConfigureAwait(false);

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
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-response.json"))));

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(mockHttpManager);
            var regionDiscovered = await regionDiscoveryProvider.getRegionAsync().ConfigureAwait(false);

            Assert.IsNotNull(regionDiscovered);
            Assert.AreEqual(Region, regionDiscovered);
            
        }

        [TestMethod]
        public async Task ErrorResponseFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound)); 

            try
            {
                IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(mockHttpManager);
                var region = await regionDiscoveryProvider.getRegionAsync().ConfigureAwait(false);
            }
            catch (MsalClientException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
            }
        }

        private void AddMockedResponse(HttpResponseMessage responseMessage)
        {
            mockHttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "http://169.254.169.254/metadata/instance/compute/api-version=2019-06-01",
                        ExpectedRequestHeaders = new Dictionary<string, string>
                         {
                            {"Metadata", "true"}
                         },
                        ResponseMessage = responseMessage
                    });
        }
    }
}
