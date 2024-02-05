// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Test.Common.Core.Mocks;
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

        public static IEnumerable<object[]> GetMtlsInvalidResourceErrorData()
        {
            // Use Func<string> for dynamic error data
            
            yield return new object[]
            {
                new Func<string>(() => MockHelpers.GetMtlsInvalidResourceError()),
                "https://graph.microsoft.com/user.read",
                TestConstants.InvalidResourceError,
                "invalid_resource"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.GetMtlsInvalidScopeError70011()),
                "user.read",
                TestConstants.InvalidScopeError70011,
                "invalid_scope"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.GetMtlsInvalidScopeError1002012()),
                "user.read",
                TestConstants.InvalidScopeError1002012,
                "invalid_scope"
            };
        }

        public static IEnumerable<object[]> GetMtlsErrorData()
        {
            yield return new object[]
            {
                new Func<string>(() => MockHelpers.InvalidTenantError900023()),
                TestConstants.InvalidTenantError900023,
                "invalid_request"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.WrongTenantError700016()),
                TestConstants.WrongTenantError700016,
                "unauthorized_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.WrongMtlsUrlError50171()),
                TestConstants.WrongMtlsUrlError50171,
                "invalid_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.SendTenantIdInCredentialValueError50027()),
                TestConstants.SendTenantIdInCredentialValueError50027,
                "invalid_request"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.BadCredNoIssError90014()),
                TestConstants.BadCredNoIssError90014,
                "invalid_request"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.BadCredNoAudError90014()),
                TestConstants.BadCredNoAudError90014,
                "invalid_request"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.BadCredBadAlgError5002738()),
                TestConstants.BadCredBadAlgError5002738,
                "invalid_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.BadCredMissingSha1Error5002723()),
                TestConstants.BadCredMissingSha1Error5002723,
                "invalid_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.BadTimeRangeError700024()),
                TestConstants.BadTimeRangeError700024,
                "invalid_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.IdentifierMismatchError700021()),
                TestConstants.IdentifierMismatchError700021,
                "invalid_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.MissingCertError392200()),
                TestConstants.MissingCertError392200,
                "invalid_request"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.ExpiredCertError392204()),
                TestConstants.ExpiredCertError392204,
                "invalid_client"
            };

            yield return new object[]
            {
                new Func<string>(() => MockHelpers.CertMismatchError500181()),
                TestConstants.CertMismatchError500181,
                "invalid_request"
            };
        }

    }
}
