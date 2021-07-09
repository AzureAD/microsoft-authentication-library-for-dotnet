// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
        public void AuthorityMismatchTest()
        {
            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(s_utidAuthority);
            var ex = AssertException.Throws<MsalClientException>(() =>
                Authority.CreateAuthorityForRequest(_testRequestContext, s_b2cAuthority, null));

            Assert.AreEqual(MsalError.AuthorityTypeMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public void DefaultAuthorityDifferentTypeTest()
        {
            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(s_commonAuthority);
            var ex = Assert.ThrowsException<MsalClientException>(
                () => Authority.CreateAuthorityForRequest(_testRequestContext, s_b2cAuthority, null));

            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public void DifferentHosts()
        {
            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.HttpManager = _harness.HttpManager;
            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(s_commonAuthority);
            var ex = Assert.ThrowsException<MsalClientException>(
                () =>
                Authority.CreateAuthorityForRequest(_testRequestContext, s_ppeAuthority, null));
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex.ErrorCode);

            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(s_ppeAuthority);
            var ex2 = Assert.ThrowsException<MsalClientException>(
              () => Authority.CreateAuthorityForRequest(_testRequestContext, s_commonAuthority, null));
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex2.ErrorCode);

            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority, true));
            var ex3 = Assert.ThrowsException<MsalClientException>(
             () => Authority.CreateAuthorityForRequest(
                 _testRequestContext,
                 AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority2, true),
                 null));
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex3.ErrorCode);

            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(AuthorityInfo.FromAuthorityUri(TestConstants.B2CAuthority, true));
            var ex4 = Assert.ThrowsException<MsalClientException>(
               () => Authority.CreateAuthorityForRequest(
                   _testRequestContext,
                   AuthorityInfo.FromAuthorityUri(TestConstants.B2CCustomDomain, true),
                   null));
            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex4.ErrorCode);

            //Checking for aliased authority. Should not throw exception
            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.SetAuthorityInfoForTest(s_commonNetAuthority);
            var authority = Authority.CreateAuthorityForRequest(_testRequestContext, s_commonAuthority);
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
            requestContext.ServiceBundle.Config.SetAuthorityInfoForTest(config);
            var resultAuthority = Authority.CreateAuthorityForRequest(requestContext, request, accountTid);
            Assert.AreEqual(resultTid, resultAuthority.TenantId);
        }
    }
}
