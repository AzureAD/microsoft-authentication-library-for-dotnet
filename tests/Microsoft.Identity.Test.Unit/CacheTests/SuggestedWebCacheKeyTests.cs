using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class SuggestedWebCacheKeyTests
    {
        private IServiceBundle _serviceBundle;

        [TestInitialize]
        public void TestInitialize()
        {
            this._serviceBundle = TestCommon.CreateDefaultServiceBundle();
        }

        [TestMethod]
        public void TestCacheKeyForADFSAuthority()
        {
            // Arrange
            var appTokenCache = new TokenCache(this._serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(this._serviceBundle , Guid.NewGuid());
            var authority = Authority.CreateAuthority(TestConstants.ADFSAuthority, true);

            requestContext.ServiceBundle.Config.AuthorityInfo = authority.AuthorityInfo;
                


            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenForClient,                
            };

            var parameters = new AuthenticationRequestParameters(
                this._serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters, 
                requestContext,
                authority);


            // Act
            var actualKey = SuggestedWebCacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = $"{this._serviceBundle.Config.ClientId}__AppTokenCache";
            Assert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestCacheKeyForTenantAuthority()
        {
            // Arrange
            const string tenantId = TestConstants.AadTenantId;
            var appTokenCache = new TokenCache(this._serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(this._serviceBundle , Guid.NewGuid());
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: tenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenForClient,
                AuthorityOverride = tenantAuthority
            };

            var parameters = new AuthenticationRequestParameters(
                this._serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters, 
                requestContext, 
                Authority.CreateAuthority(tenantAuthority));


            // Act
            var actualKey = SuggestedWebCacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = $"{this._serviceBundle.Config.ClientId}_{tenantId}_AppTokenCache";
            Assert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestCacheKeyForRemoveAccount()
        {
            // Arrange
            const string tenantId = TestConstants.AadTenantId;
            var appTokenCache = new TokenCache(this._serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(this._serviceBundle, Guid.NewGuid());
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: tenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.RemoveAccount,
                AuthorityOverride = tenantAuthority
            };

            var parameters = new AuthenticationRequestParameters(
                this._serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                Authority.CreateAuthority(tenantAuthority),
                TestConstants.HomeAccountId);


            // Act
            var actualKey = SuggestedWebCacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = parameters.HomeAccountId;
            Assert.AreEqual(expectedKey, actualKey);
        }
    }
}
