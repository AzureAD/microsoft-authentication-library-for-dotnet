// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    public class AuthorityTests : TestBase
    {
        private static readonly Authority s_commonAuthority = Authority.CreateAuthority(TestConstants.AuthorityCommonTenant, true);
        private static readonly string s_ppeCommonUri = $@"https://{TestConstants.PpeEnvironment}/{TestConstants.TenantId}";
        private static readonly Authority s_ppeAuthority = Authority.CreateAuthority(s_ppeCommonUri, true);
        private static readonly string s_ppeOrgCommonUri = $@"https://{TestConstants.PpeOrgEnvironment}/{TestConstants.TenantId}";
        private static readonly Authority s_ppeOrgAuthority = Authority.CreateAuthority(s_ppeOrgCommonUri, true);
        private static readonly Authority s_utidAuthority = Authority.CreateAuthority(TestConstants.AuthorityUtidTenant, true);
        private static readonly Authority s_utid2Authority = Authority.CreateAuthority(TestConstants.AuthorityUtid2Tenant, true);
        private static readonly Authority s_b2cAuthority = Authority.CreateAuthority(TestConstants.B2CAuthority, true);
        private static readonly Authority s_commonNetAuthority = Authority.CreateAuthority(TestConstants.PrefCacheAuthorityCommonTenant, true);

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

        [DataTestMethod]
        [DataRow(TestConstants.ADFSAuthority)]
        [DataRow(TestConstants.B2CAuthority)]
        [DataRow(TestConstants.GenericAuthority)]
        public void WithTenantId_Exceptions(string inputAuthority)
        {
            var app1 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAdfsAuthority(inputAuthority)
                .WithClientSecret("secret")
                .Build();

            var ex1 = AssertException.Throws<MsalClientException>(() =>
                app1
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                    .WithTenantId(TestConstants.TenantId));

            Assert.AreEqual(ex1.ErrorCode, MsalError.TenantOverrideNonAad);
        }

        [DataTestMethod]
        [DataRow(TestConstants.DstsAuthorityCommon)]
        [DataRow(TestConstants.DstsAuthorityTenanted)]
        [DataRow(TestConstants.CiamAuthorityMainFormat)]
        [DataRow(TestConstants.CiamAuthorityWithFriendlyName)]
        [DataRow(TestConstants.CiamAuthorityWithGuid)]
        public void WithTenantId_NonAad(string inputAuthority)
        {
            var app = ConfidentialClientApplicationBuilder
               .Create(TestConstants.ClientId)
               .WithAuthority(inputAuthority)
               .WithClientSecret("secret")
               .Build();

            var parameterBuilder = app
                .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                .WithTenantId(TestConstants.TenantId);

            Assert.AreEqual(
                new Uri(inputAuthority).Host, 
                parameterBuilder.CommonParameters.AuthorityOverride.Host, 
                "The host should have stayed the same");
            
            Assert.AreEqual(
                TestConstants.TenantId,
                AuthorityHelpers.GetTenantId(parameterBuilder.CommonParameters.AuthorityOverride.CanonicalAuthority),
                "The tenant id should have been changed");
        }
       

        [DataTestMethod]
        [DynamicData(nameof(TestData.GetAuthorityWithExpectedTenantId), typeof(TestData), DynamicDataSourceType.Method)]
        public void AADWithTenantId_Success(Uri authorityValue, string tenantId)
        {
            // Ignore authorityValue, it's just that we don't need to create another TestData method

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithClientSecret("secret")
                .Build();

            var parameterBuilder = app
                .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                .WithTenantId(tenantId);

            // Verify Host still matches the original Authority
            Assert.AreEqual(new Uri(TestConstants.AuthorityCommonTenant).Host, parameterBuilder.CommonParameters.AuthorityOverride.Host);

            // Verify the Tenant Id matches
            Assert.AreEqual(tenantId, AuthorityHelpers.GetTenantId(parameterBuilder.CommonParameters.AuthorityOverride.CanonicalAuthority));
        }

        [DataTestMethod]
        [DynamicData(nameof(TestData.GetAuthorityWithExpectedTenantId), typeof(TestData), DynamicDataSourceType.Method)]
        public void AADWithTenantIdFromAuthority_Success(Uri authorityValue, string expectedTenantId)
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithClientSecret("secret")
                .Build();

            var parameterBuilder = app
                .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                .WithTenantIdFromAuthority(authorityValue);

            // Verify Host still matches the original Authority
            Assert.AreEqual(new Uri(TestConstants.AuthorityCommonTenant).Host, parameterBuilder.CommonParameters.AuthorityOverride.Host);

            // Verify the Tenant Id matches
            Assert.AreEqual(expectedTenantId, AuthorityHelpers.GetTenantId(parameterBuilder.CommonParameters.AuthorityOverride.CanonicalAuthority));
        }

        [DataTestMethod]
        [DataRow(null)]
        public void WithTenantIdFromAuthority_NullUriAuthority_Failure(Uri authorityValue)
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithClientSecret("secret")
                .Build();

            AssertException.Throws<ArgumentNullException>(() =>
                app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                    .WithTenantIdFromAuthority(authorityValue));
        }

        [TestMethod]
        public void VerifyAuthorityTest()
        {
            VerifyAuthority(
                configAuthority: s_commonAuthority,
                requestAuthority: null,
                account: null,
                expectedTenantId: "common",
                _testRequestContext);

            VerifyAuthority(
               configAuthority: s_commonAuthority,
               requestAuthority: s_commonAuthority,
               account: null,
               expectedTenantId: "common",
               _testRequestContext);

            VerifyAuthority(
              configAuthority: s_commonAuthority,
              requestAuthority: s_commonAuthority,
              account: new Account(TestConstants.s_userIdentifier, "username", s_commonAuthority.AuthorityInfo.Host),
              expectedTenantId: TestConstants.Utid,
              _testRequestContext);

            VerifyAuthority(
             configAuthority: s_commonAuthority,
             requestAuthority: s_utidAuthority,
             account: null,
             expectedTenantId: TestConstants.Utid,
             _testRequestContext);

            VerifyAuthority(
             configAuthority: s_commonAuthority,
             requestAuthority: s_utid2Authority,
             account: new Account(TestConstants.s_userIdentifier, "username", s_utid2Authority.AuthorityInfo.Host),
             expectedTenantId: TestConstants.Utid2,
             _testRequestContext);

            VerifyAuthority(
                configAuthority: s_commonAuthority,
                requestAuthority: null,
                account: PublicClientApplication.OperatingSystemAccount,
                expectedTenantId: "common",
                _testRequestContext,
                multiCloudSupport: true);
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
                () => Authority.CreateAuthorityForRequestAsync(_testRequestContext, s_ppeOrgAuthority.AuthorityInfo, null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex.ErrorCode);

            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _testRequestContext.ServiceBundle.Config.Authority = s_ppeOrgAuthority;
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
                () => Authority.CreateAuthorityForRequestAsync(
                   _testRequestContext,
                   AuthorityInfo.FromAuthorityUri(TestConstants.B2CCustomDomain, true),
                   null)).ConfigureAwait(false);
            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex4.ErrorCode);
        }

        [TestMethod]
        public async Task DifferentHostsWithAliasedAuthorityAsync()
        {
            //Checking for aliased authority. Should not throw exception when a developer configures an authority on the application
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
            Authority configAuthority,
            Authority requestAuthority,
            IAccount account,
            string expectedTenantId,
            RequestContext requestContext,
            bool multiCloudSupport = false)
        {
            requestContext.ServiceBundle.Config.Authority = configAuthority;
            requestContext.ServiceBundle.Config.MultiCloudSupportEnabled = multiCloudSupport;
            var resultAuthority = Authority.CreateAuthorityForRequestAsync(requestContext, requestAuthority?.AuthorityInfo, account).Result;
            Assert.AreEqual(expectedTenantId, resultAuthority.TenantId);
        }
    }
}
