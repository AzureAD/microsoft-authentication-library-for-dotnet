// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    public class AuthorityTests
    {
        private static readonly AuthorityInfo s_commonAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true);
        static string s_ppeCommonUri = $@"https://{TestConstants.PpeEnvironment}/{TestConstants.TenantId}";
        private static readonly AuthorityInfo s_ppeAuthority =
          AuthorityInfo.FromAuthorityUri(s_ppeCommonUri, true);
        private static readonly AuthorityInfo s_utidAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtidTenant, true);
        private static readonly AuthorityInfo s_utid2Authority =
            AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtid2Tenant, true);
        private static readonly AuthorityInfo s_b2cAuthority =
            AuthorityInfo.FromAuthorityUri(TestConstants.B2CAuthority, true);

        [TestMethod]
        public void VerifyAuthorityTest()
        {
            var utid = TestConstants.Utid;
            var utid2 = TestConstants.Utid2;

            VerifyAuthority(
                config: s_commonAuthority,
                request: null,
                accountTid: null,
                resultTid: "common");

            VerifyAuthority(
               config: s_commonAuthority,
               request: s_commonAuthority,
               accountTid: null,
               resultTid: "common");

            VerifyAuthority(
              config: s_commonAuthority,
              request: s_commonAuthority,
              accountTid: utid,
              resultTid: utid);

            VerifyAuthority(
             config: s_commonAuthority,
             request: s_utidAuthority,
             accountTid: null,
             resultTid: utid);

            VerifyAuthority(
             config: s_commonAuthority,
             request: s_utid2Authority,
             accountTid: utid,
             resultTid: utid2);
        }

        [TestMethod]
        public void AuthorityMismatchTest()
        {
            var ex = AssertException.Throws<MsalClientException>(() =>
                Authority.CreateAuthorityForRequest(s_utidAuthority, s_b2cAuthority, null));

            Assert.AreEqual(MsalError.AuthorityTypeMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public void DefaultAuthorityDifferentTypeTest()
        {
            var ex = Assert.ThrowsException<MsalClientException>(
                () => Authority.CreateAuthorityForRequest(s_commonAuthority, s_b2cAuthority, null));

            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex.ErrorCode);
        }

        [TestMethod]
        public void DifferentHosts()
        {
            var ex = Assert.ThrowsException<MsalClientException>(
                () => Authority.CreateAuthorityForRequest(s_commonAuthority, s_ppeAuthority, null));
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex.ErrorCode);

            var ex2 = Assert.ThrowsException<MsalClientException>(
              () => Authority.CreateAuthorityForRequest(s_ppeAuthority, s_commonAuthority, null));
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex2.ErrorCode);

            var ex3 = Assert.ThrowsException<MsalClientException>(
             () => Authority.CreateAuthorityForRequest(
                 AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority, true),
                 AuthorityInfo.FromAdfsAuthority(TestConstants.ADFSAuthority2, true),
                 null));
            Assert.AreEqual(MsalError.AuthorityHostMismatch, ex3.ErrorCode);

            var ex4 = Assert.ThrowsException<MsalClientException>(
               () => Authority.CreateAuthorityForRequest(
                   AuthorityInfo.FromAuthorityUri(TestConstants.B2CAuthority, true),
                   AuthorityInfo.FromAuthorityUri(TestConstants.B2CCustomDomain, true),
                   null));
            Assert.AreEqual(MsalError.B2CAuthorityHostMismatch, ex4.ErrorCode);
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
            string resultTid)
        {
            var resultAuthority = Authority.CreateAuthorityForRequest(config, request, accountTid);
            Assert.AreEqual(resultTid, resultAuthority.TenantId);
        }
    }
}
