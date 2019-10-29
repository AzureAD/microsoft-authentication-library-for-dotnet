// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2;
using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.Identity.Client.CacheV2.Impl.InMemory;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    [TestClass]
    public class TokenCacheV2Tests : TestBase
    {
        private InMemoryCachePathStorage _cachePathStorage;
        private FileSystemCredentialPathManager _credentialPathManager;
        private StorageManager _storageManager;
        private PathStorageWorker _storageWorker;

        private const string TheSecret = "the_secret";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            _cachePathStorage = new InMemoryCachePathStorage();
            _credentialPathManager = new FileSystemCredentialPathManager(serviceBundle.PlatformProxy.CryptographyManager);
            _storageWorker = new PathStorageWorker(_cachePathStorage, _credentialPathManager);
            _storageManager = new StorageManager(serviceBundle.PlatformProxy, _storageWorker);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExactScopesMatchedAccessTokenTest()
        {
            var atItem = Credential.CreateAccessToken(
                TestConstants.HomeAccountId,
                TestConstants.ProductionPrefNetworkEnvironment,
                new Uri(TestConstants.AuthorityTestTenant).GetRealm(),
                TestConstants.ClientId,
                ScopeUtils.JoinScopes(TestConstants.s_scope),
                TimeUtils.GetSecondsFromEpochNow(),
                TimeUtils.GetSecondsFromEpochNow() + MsalCacheV2TestConstants.ValidExpiresIn,
                TimeUtils.GetSecondsFromEpochNow() + MsalCacheV2TestConstants.ValidExtendedExpiresIn,
                TheSecret,
                string.Empty);

            _storageWorker.WriteCredentials(
                new List<Credential>
                {
                    atItem
                });

            using (var harness = CreateTestHarness())
            {
                // TODO:  In MSALC++, the request parameters only really needs the
                // Authority URI itself since the cache isn't meant to
                // do ANY network calls.
                // So it would be great if we could reduce the complexity/dependencies
                // here and do any of the validated authority cache / instance discovery
                // outside of the context of the authentication parameters and
                // cache interaction and just track the authority we're using...

                // AccountId = TestConstants.HomeAccountId,
                // Authority = new Uri(TestConstants.AuthorityTestTenant),

                var cacheManager = new CacheManager(_storageManager, harness.CreateAuthenticationRequestParameters(
                                                        TestConstants.AuthorityTestTenant,
                                                        new SortedSet<string>(MsalCacheV2TestConstants.Scope),
                                                        new TokenCache(harness.ServiceBundle, false),
                                                        account: TestConstants.s_user));

                Assert.IsTrue(cacheManager.TryReadCache(out var tokenResponse, out var accountResponse));
                Assert.IsNotNull(tokenResponse);
                Assert.IsNull(accountResponse);

                Assert.AreEqual(TheSecret, tokenResponse.AccessToken);
            }
        }
    }
}
