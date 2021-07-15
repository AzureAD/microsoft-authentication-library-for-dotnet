// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    public class AuthorityTests : TestBase
    {
        private static readonly AuthorityInfo s_commonAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true);
        static string s_ppeCommonUri = $@"https://{TestConstants.PPEEnvironment}/{TestConstants.TenantId}";
        private static readonly AuthorityInfo s_ppeAuthority =
          AuthorityInfo.FromAuthorityUri(s_ppeCommonUri, true);
        private static readonly AuthorityInfo s_utidAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtidTenant, true);
        private static readonly AuthorityInfo s_utid2Authority =
            AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtid2Tenant, true);
        private static readonly AuthorityInfo s_b2cAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.B2CAuthority, true);
        private static readonly AuthorityInfo s_commonNetAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.PrefCacheAuthorityCommonTenant, true);

        private MockHttpAndServiceBundle _harness;
        private RequestContext _testRequestContext;

        [TestInitialize]
        public override void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            base.TestInitialize();
            _harness = base.CreateTestHarness();
            _testRequestContext = new RequestContext(
                _harness.ServiceBundle,
                Guid.NewGuid());
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            _harness.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        public void VerifyAuthorityTest()
        {
            var utid = TestConstants.Utid;
            var utid2 = TestConstants.Utid2;

            VerifyAuthority(
                config: s_commonAuthority,
                request: null,
                accountTid: null,
                resultTid: "common",
                _testRequestContext);

            VerifyAuthority(
               config: s_commonAuthority,
               request: s_commonAuthority,
               accountTid: null,
               resultTid: "common",
               _testRequestContext);

            VerifyAuthority(
              config: s_commonAuthority,
              request: s_commonAuthority,
              accountTid: utid,
              resultTid: utid,
              _testRequestContext);

            VerifyAuthority(
             config: s_commonAuthority,
             request: s_utidAuthority,
             accountTid: null,
             resultTid: utid,
             _testRequestContext);

            VerifyAuthority(
             config: s_commonAuthority,
             request: s_utid2Authority,
             accountTid: utid,
             resultTid: utid2,
             _testRequestContext);
        }

        [TestMethod]
        public async Task AuthorityMismatchTestAsync()
        {
            _testRequestContext.ServiceBundle.Config.AuthorityInfo = s_utidAuthority;
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_b2cAuthority, null))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.AuthorityTypeMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public async Task DefaultAuthorityDifferentTypeTestAsync()
        {
            _testRequestContext.ServiceBundle.Config.AuthorityInfo = s_commonAuthority;
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_b2cAuthority, null)).ConfigureAwait(false);

            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public async Task DifferentHostsAsync()
        {
            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.HttpManager = _harness.HttpManager;
            _testRequestContext.ServiceBundle.Config.AuthorityInfo = s_commonAuthority;
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_ppeAuthority, null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex.ErrorCode);

            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.AuthorityInfo = s_ppeAuthority;
            var ex2 = await Assert.ThrowsExceptionAsync<MsalClientException>(
              () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_commonAuthority, null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex2.ErrorCode);

            _testRequestContext.ServiceBundle.Config.AuthorityInfo = AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority, true);
            var ex3 = await Assert.ThrowsExceptionAsync<MsalClientException>(
             () => Authority.CreateAuthorityForRequestAsync(
                 _testRequestContext,
                 AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority2, true),
                 null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex3.ErrorCode);

            _testRequestContext.ServiceBundle.Config.AuthorityInfo = AuthorityInfo.FromAuthorityUri(TestConstants.B2CAuthority, true);
            var ex4 = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () =>  Authority.CreateAuthorityForRequestAsync(
                   _testRequestContext,
                   AuthorityInfo.FromAuthorityUri(TestConstants.B2CCustomDomain, true),
                   null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex4.ErrorCode);
        }

        [TestMethod]
        public async Task DifferentHostsWithAliasedAuthorityAsync()
        {
            //Checking for aliased authority. Should not throw exception whan a developer configures an authority on the application
            //but uses a different authority that is a known alias of the previously configured one.
            //See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2736
            _harness.HttpManager.AddInstanceDiscoveryMockHandler(TestConstants.PrefCacheAuthorityCommonTenant);
            _testRequestContext.ServiceBundle.Config.HttpManager = _harness.HttpManager;
            _testRequestContext.ServiceBundle.Config.AuthorityInfo = s_commonNetAuthority;
            var authority = await Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_commonAuthority).ConfigureAwait(false);
            Assert.AreEqual(s_commonNetAuthority.CanonicalAuthority, authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        public void IsDefaultAuthorityTest()
        {
            Assert.IsTrue(
                Authority.CreateAuthority(ClientApplicationBase.DefaultAuthority)
                .AuthorityInfo.IsDefaultAuthority);

            Assert.IsFalse(s_utidAuthority.IsDefaultAuthority);
            Assert.IsFalse(s_b2cAuthority.IsDefaultAuthority);
        }

        private static void VerifyAuthority(
            AuthorityInfo config,
            AuthorityInfo request,
            string accountTid,
            string resultTid,
            RequestContext requestContext)
        {
            requestContext.ServiceBundle.Config.AuthorityInfo = config;
            var resultAuthority = Authority.CreateAuthorityForRequestAsync(requestContext, request, accountTid).Result;
            Assert.AreEqual(resultTid, resultAuthority.TenantId);
        }
    }
}
