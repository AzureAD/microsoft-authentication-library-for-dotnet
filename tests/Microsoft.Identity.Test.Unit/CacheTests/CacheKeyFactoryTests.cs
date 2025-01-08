// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CacheKeyFactoryTests
    {
        private IServiceBundle _serviceBundle;

        [TestInitialize]
        public void TestInitialize()
        {
            _serviceBundle = TestCommon.CreateDefaultServiceBundle();
        }

        [TestMethod]
        public void TestCacheKeyForManagedIdentity()
        {
            // Arrange
            var appTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid(), null);
            var authority = Authority.CreateAuthority("https://login.microsoft.com/managed_identity", true);
            requestContext.ServiceBundle.Config.Authority = authority;
            requestContext.ServiceBundle.Config.ClientId = Constants.ManagedIdentityDefaultClientId;

            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity,
            };

            var parameters = new AuthenticationRequestParameters(
                _serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                authority);

            // Act
            var actualKey = CacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = $"{Constants.ManagedIdentityDefaultClientId}_{Constants.ManagedIdentityDefaultTenant}_AppTokenCache";
            Assert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestCacheKeyForADFSAuthority()
        {
            // Arrange
            var appTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid(), null);
            var authority = Authority.CreateAuthority(TestConstants.ADFSAuthority, true);

            requestContext.ServiceBundle.Config.Authority = authority;

            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenForClient,
            };

            var parameters = new AuthenticationRequestParameters(
                _serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                authority);

            // Act
            var actualKey = CacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = $"{_serviceBundle.Config.ClientId}__AppTokenCache";
            Assert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestCacheKeyForTenantAuthority()
        {
            // Arrange
            var appTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid(), null);
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: TestConstants.AadTenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenForClient,
                AuthorityOverride = tenantAuthority
            };

            var parameters = new AuthenticationRequestParameters(
                _serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                Authority.CreateAuthority(tenantAuthority));

            // Act
            var actualKey = CacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            var expectedKey = $"{_serviceBundle.Config.ClientId}_{TestConstants.AadTenantId}_AppTokenCache";
            Assert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestCacheKeyForRemoveAccount()
        {
            // Arrange
            var appTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: true);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid(), null);
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: TestConstants.AadTenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.RemoveAccount,
                AuthorityOverride = tenantAuthority
            };

            var parameters = new AuthenticationRequestParameters(
                _serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                Authority.CreateAuthority(tenantAuthority),
                TestConstants.HomeAccountId)
            {
                Account = new Account(TestConstants.HomeAccountId, TestConstants.Username, TestConstants.ProductionPrefCacheEnvironment)
            };

            // Act
            var actualKey = CacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            Assert.AreEqual(parameters.HomeAccountId, actualKey);
        }

        [TestMethod]
        public void PartitionKeyForCache()
        {
            var cache = new TokenCache(_serviceBundle, isApplicationTokenCache: false);
            var accessor = (cache as ITokenCacheInternal).Accessor;
            TokenCacheHelper.PopulateCache((cache as ITokenCacheInternal).Accessor);

            var at = accessor.GetAllAccessTokens().First();
            var rt = accessor.GetAllRefreshTokens().First();
            var idt = accessor.GetAllIdTokens().First();
            var acc = accessor.GetAllAccounts().First();

            Assert.AreEqual(at.HomeAccountId, CacheKeyFactory.GetKeyFromCachedItem(at));
            Assert.AreEqual(rt.HomeAccountId, CacheKeyFactory.GetKeyFromCachedItem(rt));
            Assert.AreEqual(idt.HomeAccountId, CacheKeyFactory.GetKeyFromCachedItem(idt));
            Assert.AreEqual(acc.HomeAccountId, CacheKeyFactory.GetKeyFromCachedItem(acc));

            at = at.WithUserAssertion("at_hash");            
            rt.OboCacheKey = "rt_hash";
            Assert.AreEqual("at_hash", CacheKeyFactory.GetKeyFromCachedItem(at));
            Assert.AreEqual("rt_hash", CacheKeyFactory.GetKeyFromCachedItem(rt));
            Assert.AreEqual(idt.HomeAccountId, CacheKeyFactory.GetKeyFromCachedItem(idt));
            Assert.AreEqual(acc.HomeAccountId, CacheKeyFactory.GetKeyFromCachedItem(acc));
        }

        [TestMethod]
        public void TestCacheKeyForObo()
        {
            // Arrange
            var userTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: false);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid(), null);
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: TestConstants.AadTenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenOnBehalfOf,
                AuthorityOverride = tenantAuthority
            };

            UserAssertion userAssertion = new UserAssertion(TestConstants.UserAssertion);

            var parameters = new AuthenticationRequestParameters(
                _serviceBundle,
                userTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                Authority.CreateAuthority(tenantAuthority))
            {
                UserAssertion = userAssertion
            };

            // Act
            var actualKey = CacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            Assert.AreEqual(userAssertion.AssertionHash, actualKey);
        }

        [TestMethod]
        public void TestCacheKeyForObo_WithCacheKey()
        {
            // Arrange
            var userTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: false);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid(), null);
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: TestConstants.AadTenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.InitiateLongRunningObo,
                AuthorityOverride = tenantAuthority
            };

            string oboCacheKey = "obo-cache-key";

            var parameters = new AuthenticationRequestParameters(
                _serviceBundle,
                userTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                Authority.CreateAuthority(tenantAuthority))
            {
                UserAssertion = new UserAssertion(TestConstants.UserAssertion),
                LongRunningOboCacheKey = oboCacheKey
            };

            // Act
            var actualKey = CacheKeyFactory.GetKeyFromRequest(parameters);

            // Assert
            Assert.IsNotNull(actualKey);
            Assert.AreEqual(oboCacheKey, actualKey);
        }
    }
}
