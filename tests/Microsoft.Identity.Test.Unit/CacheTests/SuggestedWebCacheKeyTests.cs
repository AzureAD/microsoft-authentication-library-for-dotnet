using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
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
            requestContext.ServiceBundle.Config.AuthorityInfo = 
                AuthorityInfo.FromAuthorityUri(TestConstants.ADFSAuthority, true);

            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenForClient,                
            };
            var parameters = new AuthenticationRequestParameters(
                this._serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters, requestContext);


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
                acquireTokenCommonParameters, requestContext);


            // Act
            var actualKey = SuggestedWebCacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = $"{this._serviceBundle.Config.ClientId}_{tenantId}_AppTokenCache";
            Assert.AreEqual(expectedKey, actualKey);
        }
    }
}
