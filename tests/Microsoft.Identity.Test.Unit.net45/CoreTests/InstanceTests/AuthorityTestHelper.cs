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
            var httpManager = new MockHttpManager();
            var appConfig = new ApplicationConfiguration()
            {
                HttpManager = httpManager,
                AuthorityInfo = AuthorityInfo.FromAuthorityUri(uri, false)
            };

            var serviceBundle = ServiceBundle.Create(appConfig);

            Authority authority = Authority.CreateAuthority(
                serviceBundle,
                uri);

            return authority;
        }

        internal static void AuthorityDoesNotUpdateTenant(string authorityUri, string actualTenant)
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(authorityUri);
            Assert.AreEqual(actualTenant, authority.GetTenantId());

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual(actualTenant, authority.GetTenantId());
            Assert.AreEqual(updatedAuthority, authorityUri);

            authority.UpdateWithTenant("other_tenant_id_2");
            Assert.AreEqual(authority.AuthorityInfo.CanonicalAuthority, authorityUri);
        }

    }
}
