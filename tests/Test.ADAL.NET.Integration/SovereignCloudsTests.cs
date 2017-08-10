using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Unit.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class SovereignCloudsTests
    {
        private PlatformParameters platformParameters;

        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Sovereign user use world wide authority")]
        public async Task SovereignUserWorldWideAuthorityIntegrationTest()
        {
            const string testCloudInstanceName = "some-sovereign-cloud";
            const string sovereignAuthorityHostPrefix = "login.";
            const string sovereignAuthorityHost = sovereignAuthorityHostPrefix + testCloudInstanceName;

            var sovereignTenantSpesificAuthority = $"https://{sovereignAuthorityHost}/{TestConstants.SomeTenantId}/";

            // creating AuthenticationContext with common Authority
            var authenticationContext =
                new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, false, new TokenCache());

            // mock value for authentication returnedUriInput, with cloud_instance_name claim
            var authReturnedUriInputMock = TestConstants.DefaultRedirectUri + "?code=some-code" + "&" +
                                           TokenResponseClaim.CloudInstanceName + "=" + testCloudInstanceName;

            MockHelpers.ConfigureMockWebUI(
                new AuthorizationResult(AuthorizationStatus.Success, authReturnedUriInputMock),
                // validate that authorizationUri passed to WebUi contains instance_aware query parameter
                new Dictionary<string, string> {{"instance_aware", "true"}});

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = 
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId,
                    TestConstants.DefaultDisplayableId, TestConstants.DefaultResource),
  
                AdditionalRequestValidation = request =>
                {
                    // make sure that Sovereign authority was used for Authorization request
                    Assert.AreEqual(sovereignAuthorityHost, request.RequestUri.Authority);
                }
            });

            var authenticationResult = await authenticationContext.AcquireTokenAsync(TestConstants.DefaultResource,
                TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, platformParameters, UserIdentifier.AnyUser, "instance_aware=true");

            // make sure that tenant spesific sovereign Authority returned to the app in AuthenticationResult
            Assert.AreEqual(sovereignTenantSpesificAuthority, authenticationResult.Authority);

            // make sure that AuthenticationContext Authority was not changed
            Assert.AreEqual(TestConstants.DefaultAuthorityCommonTenant, authenticationContext.Authority);

            // make sure AT was stored in the cache with tenant spesific Sovereign Authority in the key
            Assert.AreEqual(1, authenticationContext.TokenCache.tokenCacheDictionary.Count);
            Assert.AreEqual(sovereignTenantSpesificAuthority,
                authenticationContext.TokenCache.tokenCacheDictionary.Keys.FirstOrDefault().Authority);
        }
    }
}
