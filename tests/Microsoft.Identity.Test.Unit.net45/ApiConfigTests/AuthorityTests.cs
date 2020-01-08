// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    public class AuthorityTests
    {
        [TestMethod]
        public void VerifyAuthorityTest()
        {
            var commonAuthority = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true);
            var utidAuthority = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtidTenant, true);
            var utid2Authority = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtid2Tenant, true);
            var utid = TestConstants.Utid;
            var utid2 = TestConstants.Utid2;

            VerifyAuthority(
                config: commonAuthority,
                request: null,
                accountTid: null,
                resultTid: "common");

            VerifyAuthority(
               config: commonAuthority,
               request: commonAuthority,
               accountTid: null,
               resultTid: "common");

            VerifyAuthority(
              config: commonAuthority,
              request: commonAuthority,
              accountTid: utid,
              resultTid: utid);

            VerifyAuthority(
             config: commonAuthority,
             request: utidAuthority,
             accountTid: null,
             resultTid: utid);

            VerifyAuthority(
             config: commonAuthority,
             request: utid2Authority,
             accountTid: utid,
             resultTid: utid2);
        }

        private static void VerifyAuthority(
            AuthorityInfo config,
            AuthorityInfo request,
            string accountTid,
            string resultTid)
        {
            var resultAuthority = Authority.CreateAuthorityForRequest(config, request, accountTid);
            Assert.AreEqual(resultTid, resultAuthority.GetTenantId());
        }
    }
}
