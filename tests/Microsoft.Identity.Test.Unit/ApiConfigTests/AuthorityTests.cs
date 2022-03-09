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
        private static readonly Authority s_commonAuthority =
            Authority.CreateAuthority(TestConstants.AuthorityCommonTenant, true);
        private static readonly string s_ppeCommonUri = $@"https://{TestConstants.PpeEnvironment}/{TestConstants.TenantId}";
        private static readonly Authority s_ppeAuthority =
          Authority.CreateAuthority(s_ppeCommonUri, true);
        private static readonly Authority s_utidAuthority =
            Authority.CreateAuthority(TestConstants.AuthorityUtidTenant, true);
        private static readonly Authority s_utid2Authority =
            Authority.CreateAuthority(TestConstants.AuthorityUtid2Tenant, true);
        private static readonly Authority s_b2cAuthority =
            Authority.CreateAuthority(TestConstants.B2CAuthority, true);
        private static readonly Authority s_commonNetAuthority =
            Authority.CreateAuthority(TestConstants.PrefCacheAuthorityCommonTenant, true);

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
        public void WithTenantIdExceptions()
        {
            var app1 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAdfsAuthority(TestConstants.ADFSAuthority)
                .WithClientSecret("secret")
                .Build();

            var ex1 = AssertException.Throws<MsalClientException>(() =>
                app1
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                    .WithTenantId(TestConstants.TenantId));

            var app2 = ConfidentialClientApplicationBuilder
               .Create(TestConstants.ClientId)
               .WithB2CAuthority(TestConstants.B2CAuthority)
               .WithClientSecret("secret")
               .Build();

            var ex2 = AssertException.Throws<MsalClientException>(() =>
                app2
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                    .WithTenantId(TestConstants.TenantId));

            Assert.AreEqual(ex1.ErrorCode, MsalError.TenantOverrideNonAad);
            Assert.AreEqual(ex2.ErrorCode, MsalError.TenantOverrideNonAad);

        }

        [TestMethod]
        public void VerifyAuthorityTest()
        {
            const string utid = TestConstants.Utid;
            const string utid2 = TestConstants.Utid2;

            VerifyAuthority(
                config: s_commonAuthority,
                request: null,
                account: null,
                resultTid: "common",
                _testRequestContext);

            VerifyAuthority(
               config: s_commonAuthority,
               request: s_commonAuthority,
               account: null,
               resultTid: "common",
               _testRequestContext);

            VerifyAuthority(
              config: s_commonAuthority,
              request: s_commonAuthority,
              account: new Account(TestConstants.s_userIdentifier, "username", s_commonAuthority.AuthorityInfo.Host),              
              resultTid: utid,
              _testRequestContext);

            VerifyAuthority(
             config: s_commonAuthority,
             request: s_utidAuthority,
             account: null,
             resultTid: utid,
             _testRequestContext);

            VerifyAuthority(
             config: s_commonAuthority,
             request: s_utid2Authority,
             account: new Account(TestConstants.s_userIdentifier, "username", s_utid2Authority.AuthorityInfo.Host),
             resultTid: utid2,
             _testRequestContext);
        }

        [TestMethod]
        public async Task AuthorityMismatchTestAsync()
        {
            _testRequestContext.ServiceBundle.Config.Authority = s_utidAuthority;
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_b2cAuthority.AuthorityInfo, null))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.AuthorityTypeMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public async Task DefaultAuthorityDifferentTypeTestAsync()
        {
            _testRequestContext.ServiceBundle.Config.Authority = s_commonAuthority;
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_b2cAuthority.AuthorityInfo, null)).ConfigureAwait(false);

            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public async Task DifferentHostsAsync()
        {
            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.HttpManager = _harness.HttpManager;
            _testRequestContext.ServiceBundle.Config.Authority = s_commonAuthority;
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_ppeAuthority.AuthorityInfo, null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex.ErrorCode);

            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.Authority = s_ppeAuthority;
            var ex2 = await Assert.ThrowsExceptionAsync<MsalClientException>(
              () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_commonAuthority.AuthorityInfo, null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex2.ErrorCode);

            _testRequestContext.ServiceBundle.Config.Authority = Authority.CreateAuthority(TestConstants.ADFSAuthority, true);
            var ex3 = await Assert.ThrowsExceptionAsync<MsalClientException>(
             () => Authority.CreateAuthorityForRequestAsync(
                 _testRequestContext,
                 AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority2, true),
                 null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex3.ErrorCode);

            _testRequestContext.ServiceBundle.Config.Authority = Authority.CreateAuthority(TestConstants.B2CAuthority, true);
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
            _testRequestContext.ServiceBundle.Config.Authority = s_commonNetAuthority;
            var authority = await Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_commonAuthority.AuthorityInfo).ConfigureAwait(false);
            Assert.AreEqual(s_commonNetAuthority.AuthorityInfo.CanonicalAuthority, authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        public void IsDefaultAuthorityTest()
        {
            Assert.IsTrue(
                Authority.CreateAuthority(ClientApplicationBase.DefaultAuthority)
                .AuthorityInfo.IsDefaultAuthority);

            Assert.IsFalse(s_utidAuthority.AuthorityInfo.IsDefaultAuthority);
            Assert.IsFalse(s_b2cAuthority.AuthorityInfo.IsDefaultAuthority);
        }

        private static void VerifyAuthority(
            Authority config,
            Authority request,
            IAccount account,
            string resultTid,
            RequestContext requestContext)
        {
            requestContext.ServiceBundle.Config.Authority = config;
            var resultAuthority = Authority.CreateAuthorityForRequestAsync(requestContext, request?.AuthorityInfo, account).Result;
            Assert.AreEqual(resultTid, resultAuthority.TenantId);
        }
    }
}
