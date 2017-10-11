using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;

namespace Test.ADAL.NET.Integration
{
    [TestClass]
    [DeploymentItem("TestMex.xml")]
    [DeploymentItem("WsTrustResponse.xml")]
    public class FederatedUserCredentialTests
    {
        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler());
        }

        [TestMethod]
        [Description("Test for AcquireToken with empty cache")]
        public async Task AcquireTokenWithEmptyCache_GetsTokenFromServiceTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            await context.Authenticator.UpdateFromTemplateAsync(null);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"federated\",\"domain_name\":\"microsoft.com\"," +
                                                "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                                                "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                                                "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                                                ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/mex")
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("TestMex.xml"))
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed")
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse.xml"))
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler("https://login.microsoftonline.com/common/oauth2/token")
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                    {"scope", "openid"}
                }
            });

            // Call acquire token
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            // All mocks are consumed
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
    }
}