using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Internal;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    [TestClass]
    public class ClientCredentialRequestTests
    {
        private TokenCache _cache;

        [TestInitialize]
        public void TestInitialize()
        {
            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.TokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            _cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("ClientCredentialRequestTests")]
        public async Task ForceRefreshParameterTest()
        {
            TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);

            var parameters = new AuthenticationRequestParameters
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                TokenCache = _cache,
                RequestContext = new RequestContext(Guid.Empty),
                User = new User()
                {
                    Identifier = TestConstants.UserIdentifier,
                    DisplayableId = TestConstants.DisplayableId
                }
            };
 
            var clientCredentialRequest = new ClientCredentialRequest(parameters, forceRefresh: false);
            await clientCredentialRequest.PreTokenRequest();
            Assert.IsNotNull(clientCredentialRequest.AccessTokenItem, "forceRefresh == false -> Access Token from cache was used");;

            clientCredentialRequest = new ClientCredentialRequest(parameters, forceRefresh: true);
            await clientCredentialRequest.PreTokenRequest();
            Assert.IsNull(clientCredentialRequest.AccessTokenItem, "forceRefresh == true -> Access Token from cache was not used");
        }
    }
}
