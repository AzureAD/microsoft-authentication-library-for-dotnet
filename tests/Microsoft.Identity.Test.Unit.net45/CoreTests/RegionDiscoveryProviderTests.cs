using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    [DeploymentItem("Resources\\local-imds-response.json")]
    public class RegionDiscoveryProviderTests : TestBase
    {
        private const string Region = "centralus";
        private MockHttpAndServiceBundle _harness;
        private MockHttpManager _httpManager;
        private RequestContext _testRequestContext;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            _harness = base.CreateTestHarness();
            _httpManager = _harness.HttpManager;
            _testRequestContext = new RequestContext(_harness.ServiceBundle, Guid.NewGuid());
        }

        [TestMethod]
        public async Task SuccessfulResponseFromEnvironmentVariableAsync ()
        {
            try
            {
                Environment.SetEnvironmentVariable("REGION_NAME", Region);

                IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
                InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                Assert.IsNotNull(regionalMetadata);
                Assert.AreEqual("centralus.login.microsoftonline.com", regionalMetadata.PreferredNetwork);
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

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.microsoftonline.com", regionalMetadata.PreferredNetwork);

        }

        [TestMethod]
        public async Task ErrorResponseFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound)); 

            try
            {
                IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
                InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                Assert.Fail("Exception should be thrown.");
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
            _httpManager.AddMockHandler(
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
