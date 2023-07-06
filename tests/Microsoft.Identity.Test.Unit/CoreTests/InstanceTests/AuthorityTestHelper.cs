// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    internal class AuthorityTestHelper
    {
        public static Authority CreateAuthorityFromUrl(string uri)
        {
            Authority authority = Authority.CreateAuthority(uri);

            return authority;
        }

        internal static void AuthorityDoesNotUpdateTenant(string authorityUri, string actualTenant)
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(authorityUri);
            Assert.AreEqual(actualTenant, authority.TenantId);

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id", false);
            Assert.AreEqual(actualTenant, authority.TenantId);
            Assert.AreEqual(updatedAuthority, authorityUri);

            authority = Authority.CreateAuthorityWithTenant(authority.AuthorityInfo, "other_tenant_id_2");

            Assert.AreEqual(authority.AuthorityInfo.CanonicalAuthority.AbsoluteUri, authorityUri);
        }

    }
}
