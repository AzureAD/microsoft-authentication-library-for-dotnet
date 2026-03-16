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

        /// <summary>
        /// Provides test data for scenarios involving the merging of claims and client capabilities.
        /// </summary>
        /// <remarks>
        /// Test cases include various combinations of claims, client capabilities, and the expected merged JSON result.
        /// </remarks>
        /// <returns>Enumerable of Object Arrays with Index 0 = Claims, Index 1 = Client Capabilities, Index 2 = Expected Merged JSON</returns>
        public static IEnumerable<object[]> GetClaimsAndCapabilities()
        {
            // Test case with non-empty claims, non-empty capabilities, and the expected merged JSON
            yield return new object[] { TestConstants.Claims, TestConstants.ClientCapabilities, TestConstants.ClientCapabilitiesAndClaimsJson };

            // Test case with claims containing an access token, non-empty capabilities, and the expected merged JSON
            yield return new object[] { TestConstants.ClaimsWithAccessToken, TestConstants.ClientCapabilities, TestConstants.ClientCapabilitiesAndClaimsJsonWithAccessToken };

            // Test case with empty claims, non-empty capabilities, and the expected merged JSON being the capabilities alone
            yield return new object[] { TestConstants.EmptyClaimsJson, TestConstants.ClientCapabilities, TestConstants.ClientCapabilitiesJson };

            // Test case with claims containing an additional claim, non-empty capabilities, and the expected merged JSON
            yield return new object[] { TestConstants.ClaimsWithAdditionalClaim, TestConstants.ClientCapabilities, TestConstants.MergedJsonWithAdditionalClaim };

            // Test case with claims containing an additional key, non-empty capabilities, and the expected merged JSON
            yield return new object[] { TestConstants.ClaimWithAdditionalKey, TestConstants.ClientCapabilities, TestConstants.MergedJsonWithAdditionalKey };

            // Test case with claims containing an additional key, empty capabilities, and the expected merged JSON being the claims alone
            yield return new object[] { TestConstants.ClaimWithAdditionalKey, new string[0], TestConstants.ClaimWithAdditionalKey };

            // Test case with non-empty claims, empty capabilities, and the expected merged JSON being the claims alone
            yield return new object[] { TestConstants.Claims, new string[0], TestConstants.Claims };

            // Test case with null claims, non-empty capabilities, and the expected merged JSON being the capabilities alone
            yield return new object[] { null, TestConstants.ClientCapabilities, TestConstants.ClientCapabilitiesJson };

            // Test case with non-empty claims, null capabilities, and the expected merged JSON being the claims alone
            yield return new object[] { TestConstants.Claims, null, TestConstants.Claims };

            // Test case with claims containing an access token, null capabilities, and the expected merged JSON
            yield return new object[] { TestConstants.ClaimsWithAccessToken, null, TestConstants.ClaimsWithAccessToken };

            // Test case with claims containing an additional key and access key (different order), non-empty capabilities, and the expected merged JSON
            yield return new object[] { TestConstants.ClaimWithAdditionalKeyAndAccessKey, TestConstants.ClientCapabilities, TestConstants.MergedJsonClaimWithAdditionalKeyAndAccessKey };
        }
    }
}
