// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common
{
    public static class TestData
    {
        /// <summary>Retrieves a list of Authorities and their defining Tenant Id's</summary>
        /// <remarks>Does not include non-Tenanted Authorities like B2C</remarks>
        /// <returns>Enumerable of Object Array with Index 0 = Authority URL and Index 1 = Expected Tenant Id</returns>
        public static IEnumerable<object[]> GetAuthorityWithExpectedTenantId()
        {
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityCommonTenant), ExpectedTenantId = TestConstants.Common }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityCommonPpeAuthority), ExpectedTenantId = TestConstants.Common }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityCommonTenantNotPrefAlias), ExpectedTenantId = TestConstants.Common }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityConsumerTidTenant), ExpectedTenantId = TestConstants.MsaTenantId }.ToObjectArray();

            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityConsumersTenant), ExpectedTenantId = TestConstants.Consumers }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityGuestTenant), ExpectedTenantId = TestConstants.Guest }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityGuidTenant), ExpectedTenantId = TestConstants.TenantIdNumber1 }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityGuidTenant2), ExpectedTenantId = TestConstants.TenantIdNumber2 }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityHomeTenant), ExpectedTenantId = TestConstants.Home }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityNotKnownCommon), ExpectedTenantId = TestConstants.Common }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityNotKnownTenanted), ExpectedTenantId = TestConstants.Utid }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityOrganizationsTenant), ExpectedTenantId = TestConstants.Organizations }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityRegional), ExpectedTenantId = TestConstants.TenantId }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityRegionalInvalidRegion), ExpectedTenantId = TestConstants.TenantId }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthoritySovereignCNCommon), ExpectedTenantId = TestConstants.Common }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthoritySovereignCNTenant), ExpectedTenantId = TestConstants.TenantId }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthoritySovereignDECommon), ExpectedTenantId = TestConstants.Common }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthoritySovereignDETenant), ExpectedTenantId = TestConstants.TenantId }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityTenant), ExpectedTenantId = TestConstants.TenantId }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityTestTenant), ExpectedTenantId = TestConstants.Utid }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AadAuthorityWithTestTenantId), ExpectedTenantId = TestConstants.AadTenantId }.ToObjectArray();
            yield return new AuthorityWithExpectedTenantId { Authority = new Uri(TestConstants.AuthorityWindowsNet), ExpectedTenantId = TestConstants.Utid }.ToObjectArray();
        }
    }
}
