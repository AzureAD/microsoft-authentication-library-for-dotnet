// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    /// <summary>
    /// Contains shared test constants, helper values, sample payloads, and factory methods
    /// used across MSAL unit tests.
    /// </summary>
    public static class TestConstants
    {
        /// <summary>
        /// Gets the default scope set used by tests for the primary resource.
        /// </summary>
        public static HashSet<string> s_scope
        {
            get
            {
                return new HashSet<string>(new[] { "r1/scope1", "r1/scope2" }, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the extra HTTP headers used in test requests.
        /// </summary>
        public static readonly Dictionary<string, string> s_extraHttpHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "SomeExtraHeadderKey", "SomeExtraHeadderValue" } };

        /// <summary>
        /// A space-delimited string representation of <see cref="s_scope"/>.
        /// </summary>
        public const string ScopeStr = "r1/scope1 r1/scope2";

        /// <summary>
        /// A formatted scope string template used to construct resource-specific scopes.
        /// </summary>
        public const string ScopeStrFormat = "r{0}/scope1 r{0}/scope2";

        /// <summary>
        /// The MSI resource string used in tests.
        /// </summary>
        public const string MsiResource = "scope";

        /// <summary>
        /// Gets the Microsoft Graph scopes used by tests.
        /// </summary>
        public static readonly string[] s_graphScopes = new[] { "user.read" };

        /// <summary>
        /// The lifetime, in seconds, used for JWT to AAD token calculations.
        /// </summary>
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        /// <summary>
        /// Audience used by client credential tests.
        /// </summary>
        public const string ClientCredentialAudience = "https://login.microsoftonline.com/10c419d4-4a50-45b2-aa4e-919fb84df24f/v2.0"; // ID4SLAB1 tenant

        /// <summary>
        /// Public cloud confidential client identifier used in tests.
        /// </summary>
        public const string PublicCloudConfidentialClientID = "54a2d933-8bf8-483b-a8f8-0a31924f3c1f"; // MSAL-APP-AzureADMultipleOrgs in ID4SLAB1 tenant

        /// <summary>
        /// Certificate name used by automation tests.
        /// </summary>
        public const string AutomationTestCertName = "LabAuth.MSIDLab.com";

        /// <summary>
        /// Gets additional assertion claims used by tests.
        /// </summary>
        public static Dictionary<string, string> AdditionalAssertionClaims =>
            new Dictionary<string, string>() { { "Key1", "Val1" }, { "Key2", "Val2" }, { "customClaims", "{\"xms_az_claim\": [\"GUID\", \"GUID2\", \"GUID3\"]}" } };

        /// <summary>
        /// Gets the scope set used for a second resource in cache and authority tests.
        /// </summary>
        public static readonly SortedSet<string> s_scopeForAnotherResource = new SortedSet<string>(new[] { "r2/scope1", "r2/scope2" }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the scope set used to simulate cache misses.
        /// </summary>
        public static readonly SortedSet<string> s_cacheMissScope = new SortedSet<string>(new[] { "r3/scope1", "r3/scope2" }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A space-delimited string representation of <see cref="s_scopeForAnotherResource"/>.
        /// </summary>
        public const string ScopeForAnotherResourceStr = "r2/scope1 r2/scope2";

        /// <summary>
        /// Sample UID value used in home account and user identifier tests.
        /// </summary>
        public const string Uid = "my-uid";

        /// <summary>
        /// Sample UTID value used in home account and user identifier tests.
        /// </summary>
        public const string Utid = "my-utid";

        /// <summary>
        /// Secondary sample UTID value used in tests.
        /// </summary>
        public const string Utid2 = "my-utid2";

        /// <summary>
        /// The common tenant alias.
        /// </summary>
        public const string Common = Constants.Common;

        /// <summary>
        /// The organizations tenant alias.
        /// </summary>
        public const string Organizations = Constants.Organizations;

        /// <summary>
        /// The consumers tenant alias.
        /// </summary>
        public const string Consumers = Constants.Consumers;

        /// <summary>
        /// Guest tenant name used in test authorities.
        /// </summary>
        public const string Guest = "guest";

        /// <summary>
        /// Home tenant name used in test authorities.
        /// </summary>
        public const string Home = "home";

        /// <summary>
        /// Primary test tenant identifier.
        /// </summary>
        public const string TenantId = "751a212b-4003-416e-b600-e1f48e40db9f";

        /// <summary>
        /// Secondary test tenant identifier.
        /// </summary>
        public const string TenantId2 = "aaaaaaab-aaaa-aaaa-bbbb-aaaaaaaaaaaa";

        /// <summary>
        /// Azure AD tenant identifier used in tests.
        /// </summary>
        public const string AadTenantId = "751a212b-4003-416e-b600-e1f48e40db9f";

        /// <summary>
        /// Microsoft account tenant identifier.
        /// </summary>
        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

        /// <summary>
        /// Microsoft first-party tenant identifier.
        /// </summary>
        public const string MsftTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        /// <summary>
        /// Placeholder tenant identifier used in tests.
        /// </summary>
        public const string SomeTenantId = "sometenantid";

        /// <summary>
        /// Placeholder string used in B2C-related tests.
        /// </summary>
        public const string CatsAreAwesome = "catsareawesome";

        /// <summary>
        /// Numeric tenant identifier variant used in tests.
        /// </summary>
        public const string TenantIdNumber1 = "12345679";

        /// <summary>
        /// Secondary numeric tenant identifier variant used in tests.
        /// </summary>
        public const string TenantIdNumber2 = "987654321";

        /// <summary>
        /// String tenant identifier variant used in tests.
        /// </summary>
        public const string TenantIdString = "tenantid";

        /// <summary>
        /// Azure AD authority with the primary test tenant identifier.
        /// </summary>
        public const string AadAuthorityWithTestTenantId = "https://login.microsoftonline.com/" + AadTenantId + "/";

        /// <summary>
        /// Azure AD authority with the Microsoft tenant identifier.
        /// </summary>
        public const string AadAuthorityWithMsftTenantId = "https://login.microsoftonline.com/" + MsftTenantId + "/";

        /// <summary>
        /// Gets the client assertion claims used in tests.
        /// </summary>
        public static readonly IDictionary<string, string> s_clientAssertionClaims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "client_ip", "some_ip" }, { "aud", "some_audience" } };

        /// <summary>
        /// Sample refresh token secret.
        /// </summary>
        public const string RTSecret = "someRT";

        /// <summary>
        /// Sample access token secret.
        /// </summary>
        public const string ATSecret = "some-access-token";

        /// <summary>
        /// Secondary refresh token secret.
        /// </summary>
        public const string RTSecret2 = "someRT2";

        /// <summary>
        /// Secondary access token secret.
        /// </summary>
        public const string ATSecret2 = "some-access-token2";

        /// <summary>
        /// Third refresh token secret.
        /// </summary>
        public const string RTSecret3 = "someRT3";

        /// <summary>
        /// Third access token secret.
        /// </summary>
        public const string ATSecret3 = "some-access-token3";

        /// <summary>
        /// Home account identifier composed from <see cref="Uid"/> and <see cref="Utid"/>.
        /// </summary>
        public const string HomeAccountId = Uid + "." + Utid;

        /// <summary>
        /// Preferred production network environment.
        /// </summary>
        public const string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";

        /// <summary>
        /// Preferred production cache environment.
        /// </summary>
        public const string ProductionPrefCacheEnvironment = "login.windows.net";

        // TODO: Tenant Migration - Regional endpoint may need update after migration
        // Current: centralus (old tenant), New: eastus2 (id4slab1 tenant)
        // Note: Regional endpoints may not work with new tenant due to AADSTS100007 restrictions

        /// <summary>
        /// Preferred regional production environment used in tests.
        /// </summary>
        public const string ProductionPrefRegionalEnvironment = "centralus.login.microsoft.com";

        /// <summary>
        /// Invalid regional environment used for negative tests.
        /// </summary>
        public const string ProductionPrefInvalidRegionEnvironment = "invalidregion.login.microsoft.com";

        /// <summary>
        /// Non-preferred production environment alias.
        /// </summary>
        public const string ProductionNotPrefEnvironmentAlias = "sts.windows.net";

        /// <summary>
        /// German sovereign cloud network environment.
        /// </summary>
        public const string SovereignNetworkEnvironmentDE = "login.microsoftonline.de";

        /// <summary>
        /// China sovereign cloud network environment.
        /// </summary>
        public const string SovereignNetworkEnvironmentCN = "login.partner.microsoftonline.cn";

        /// <summary>
        /// PPE environment host.
        /// </summary>
        public const string PpeEnvironment = "login.windows-ppe.net";

        /// <summary>
        /// PPE organization environment host not known to MSAL or AAD.
        /// </summary>
        public const string PpeOrgEnvironment = "login.windows-ppe.org"; //This environment is not known to MSAL or AAD

        /// <summary>
        /// Bleu sovereign cloud network environment.
        /// </summary>
        public const string SovereignNetworkEnvironmentBleu = "login.sovcloud-identity.fr";

        /// <summary>
        /// Delos sovereign cloud network environment.
        /// </summary>
        public const string SovereignNetworkEnvironmentDelos = "login.sovcloud-identity.de";

        /// <summary>
        /// Singapore government sovereign cloud network environment.
        /// </summary>
        public const string SovereignNetworkEnvironmentGovSG = "login.sovcloud-identity.sg";

        /// <summary>
        /// Unknown authority using the common tenant alias.
        /// </summary>
        public const string AuthorityNotKnownCommon = "https://sts.access.edu/" + Common + "/";

        /// <summary>
        /// Unknown authority using a tenanted path.
        /// </summary>
        public const string AuthorityNotKnownTenanted = "https://sts.access.edu/" + Utid + "/";

        /// <summary>
        /// Authority targeting the home tenant.
        /// </summary>
        public const string AuthorityHomeTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Home + "/";

        /// <summary>
        /// Authority targeting the UTID tenant.
        /// </summary>
        public const string AuthorityUtidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid + "/";

        /// <summary>
        /// Authority targeting the secondary UTID tenant.
        /// </summary>
        public const string AuthorityUtid2Tenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid2 + "/";

        /// <summary>
        /// Authority targeting the guest tenant.
        /// </summary>
        public const string AuthorityGuestTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Guest + "/";

        /// <summary>
        /// Authority targeting the common tenant.
        /// </summary>
        public const string AuthorityCommonTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Common + "/";

        /// <summary>
        /// Authority targeting the preferred regional environment.
        /// </summary>
        public const string AuthorityRegional = "https://" + ProductionPrefRegionalEnvironment + "/" + TenantId + "/";

        /// <summary>
        /// Authority targeting an invalid regional environment.
        /// </summary>
        public const string AuthorityRegionalInvalidRegion = "https://" + ProductionPrefInvalidRegionEnvironment + "/" + TenantId + "/";

        /// <summary>
        /// Authority targeting the primary test tenant.
        /// </summary>
        public const string AuthorityTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantId + "/";

        /// <summary>
        /// Common authority using a non-preferred environment alias.
        /// </summary>
        public const string AuthorityCommonTenantNotPrefAlias = "https://" + ProductionNotPrefEnvironmentAlias + "/" + Common + "/";

        /// <summary>
        /// Common authority in PPE.
        /// </summary>
        public const string AuthorityCommonPpeAuthority = "https://" + PpeEnvironment + "/" + Common + "/";

        /// <summary>
        /// Tenanted German sovereign authority.
        /// </summary>
        public const string AuthoritySovereignDETenant = "https://" + SovereignNetworkEnvironmentDE + "/" + TenantId + "/";

        /// <summary>
        /// Tenanted China sovereign authority.
        /// </summary>
        public const string AuthoritySovereignCNTenant = "https://" + SovereignNetworkEnvironmentCN + "/" + TenantId + "/";

        /// <summary>
        /// Common German sovereign authority.
        /// </summary>
        public const string AuthoritySovereignDECommon = "https://" + SovereignNetworkEnvironmentDE + "/" + Common + "/";

        /// <summary>
        /// Common China sovereign authority.
        /// </summary>
        public const string AuthoritySovereignCNCommon = "https://" + SovereignNetworkEnvironmentCN + "/" + Common + "/";

        /// <summary>
        /// Preferred cache authority for the common tenant.
        /// </summary>
        public const string PrefCacheAuthorityCommonTenant = "https://" + ProductionPrefCacheEnvironment + "/" + Common + "/";

        /// <summary>
        /// Authority targeting the organizations tenant.
        /// </summary>
        public const string AuthorityOrganizationsTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Organizations + "/";

        /// <summary>
        /// Authority targeting the consumers tenant.
        /// </summary>
        public const string AuthorityConsumersTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Consumers + "/";

        /// <summary>
        /// Authority targeting the MSA tenant identifier.
        /// </summary>
        public const string AuthorityConsumerTidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + MsaTenantId + "/";

        /// <summary>
        /// Authority using the first numeric tenant identifier.
        /// </summary>
        public const string AuthorityGuidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantIdNumber1 + "/";

        /// <summary>
        /// Authority using the second numeric tenant identifier.
        /// </summary>
        public const string AuthorityGuidTenant2 = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantIdNumber2 + "/";

        /// <summary>
        /// Authority using the preferred cache environment.
        /// </summary>
        public const string AuthorityWindowsNet = "https://" + ProductionPrefCacheEnvironment + "/" + Utid + "/";

        /// <summary>
        /// Primary ADFS authority used in tests.
        /// </summary>
        public const string ADFSAuthority = "https://fs.msidlab8.com/adfs/";

        /// <summary>
        /// Secondary ADFS authority used in tests.
        /// </summary>
        public const string ADFSAuthority2 = "https://someAdfs.com/adfs/";

        /// <summary>
        /// Tenantless DSTS authority.
        /// </summary>
        public const string DstsAuthorityTenantless = "https://some.url.dsts.core.azure-test.net/dstsv2/";

        /// <summary>
        /// Tenanted DSTS authority.
        /// </summary>
        public const string DstsAuthorityTenanted = DstsAuthorityTenantless + TenantId + "/";

        /// <summary>
        /// Common DSTS authority.
        /// </summary>
        public const string DstsAuthorityCommon = DstsAuthorityTenantless + Common + "/";

        /// <summary>
        /// Organizations DSTS authority.
        /// </summary>
        public const string DstsAuthorityOrganizations = DstsAuthorityTenantless + Organizations + "/";

        /// <summary>
        /// Consumers DSTS authority.
        /// </summary>
        public const string DstsAuthorityConsumers = DstsAuthorityTenantless + Consumers + "/";

        /// <summary>
        /// Generic OIDC authority used in tests.
        /// </summary>
        public const string GenericAuthority = "https://demo.duendesoftware.com";

        // not actually used by MSAL directly, MSAL will transform it to tenanted format

        /// <summary>
        /// Root CIAM authority format before tenant transformation.
        /// </summary>
        public const string CiamAuthorityMainFormat = "https://tenant.ciamlogin.com/";

        /// <summary>
        /// CIAM authority using a friendly tenant name.
        /// </summary>
        public const string CiamAuthorityWithFriendlyName = "https://tenant.ciamlogin.com/tenant.onmicrosoft.com";

        /// <summary>
        /// CIAM authority using a GUID tenant identifier.
        /// </summary>
        public const string CiamAuthorityWithGuid = "https://tenant.ciamlogin.com/aaaaaaab-aaaa-aaaa-cccc-aaaaaaaaaaaa";

        /// <summary>
        /// CIAM authority used by custom domain unit tests.
        /// </summary>
        public const string CiamCUDAuthority = "https://login.msidlabsciam.com/aaaaaaab-aaaa-aaaa-cccc-aaaaaaaaaaaa/v2.0";

        /// <summary>
        /// Malformed CIAM authority used in negative tests.
        /// </summary>
        public const string CiamCUDAuthorityMalformed = "https://login.msidlabsciam.com/aaaaaaab-aaaa-aaaa-cccc-aaaaaaaaaaaa";

        /// <summary>
        /// Global B2C login suffix.
        /// </summary>
        public const string B2CLoginGlobal = ".b2clogin.com";

        /// <summary>
        /// US Government B2C login suffix.
        /// </summary>
        public const string B2CLoginUSGov = ".b2clogin.us";

        /// <summary>
        /// Mooncake B2C login suffix.
        /// </summary>
        public const string B2CLoginMoonCake = ".b2clogin.cn";

        /// <summary>
        /// Blackforest B2C login suffix.
        /// </summary>
        public const string B2CLoginBlackforest = ".b2clogin.de";

        /// <summary>
        /// Custom domain used in B2C tests.
        /// </summary>
        public const string B2CLoginCustomDomain = CatsAreAwesome + ".com";

        /// <summary>
        /// B2C sign-up/sign-in policy name.
        /// </summary>
        public const string B2CSignUpSignIn = "b2c_1_susi";

        /// <summary>
        /// B2C profile policy containing a dot.
        /// </summary>
        public const string B2CProfileWithDot = "b2c.someprofile";

        /// <summary>
        /// B2C edit profile policy name.
        /// </summary>
        public const string B2CEditProfile = "b2c_1_editprofile";

        /// <summary>
        /// Default B2C environment host.
        /// </summary>
        public const string B2CEnvironment = SomeTenantId + ".b2clogin.com";

        /// <summary>
        /// B2C authority using the legacy login.microsoftonline.in format.
        /// </summary>
        public const string B2CAuthority = "https://login.microsoftonline.in/tfp/tenant/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// Standard B2C login authority.
        /// </summary>
        public const string B2CLoginAuthority = "https://" + B2CEnvironment + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// B2C authority using an incorrect host, for negative tests.
        /// </summary>
        public const string B2CLoginAuthorityWrongHost = "https://anothertenantid.b2clogin.com/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// B2C authority using a custom domain.
        /// </summary>
        public const string B2CCustomDomain = "https://" + B2CLoginCustomDomain + "/tfp/" + CatsAreAwesome + "/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// US Government B2C login authority.
        /// </summary>
        public const string B2CLoginAuthorityUsGov = "https://" + SomeTenantId + B2CLoginUSGov + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// Mooncake B2C login authority.
        /// </summary>
        public const string B2CLoginAuthorityMoonCake = "https://" + SomeTenantId + B2CLoginMoonCake + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// Blackforest B2C login authority.
        /// </summary>
        public const string B2CLoginAuthorityBlackforest = "https://" + SomeTenantId + B2CLoginBlackforest + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";

        /// <summary>
        /// Home account identifier for the B2C sign-up/sign-in policy.
        /// </summary>
        public const string B2CSuSiHomeAccountIdentifer = Uid + "-" + B2CSignUpSignIn + "." + Utid;

        /// <summary>
        /// Home account object identifier for the B2C sign-up/sign-in policy.
        /// </summary>
        public const string B2CSuSiHomeAccountObjectId = Uid + "-" + B2CSignUpSignIn;

        /// <summary>
        /// Home account identifier for the B2C profile-with-dot policy.
        /// </summary>
        public const string B2CProfileWithDotHomeAccountIdentifer = Uid + "-" + B2CProfileWithDot + "." + Utid;

        /// <summary>
        /// Home account object identifier for the B2C profile-with-dot policy.
        /// </summary>
        public const string B2CProfileWithDotHomeAccountObjectId = Uid + "-" + B2CProfileWithDot;

        /// <summary>
        /// Home account identifier for the B2C edit profile policy.
        /// </summary>
        public const string B2CEditProfileHomeAccountIdentifer = Uid + "-" + B2CEditProfile + "." + Utid;

        /// <summary>
        /// Home account object identifier for the B2C edit profile policy.
        /// </summary>
        public const string B2CEditProfileHomeAccountObjectId = Uid + "-" + B2CEditProfile;

        /// <summary>
        /// Primary client identifier used in tests.
        /// </summary>
        public const string ClientId = "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3";

        /// <summary>
        /// Secondary client identifier used in tests.
        /// </summary>
        public const string ClientId2 = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        /// <summary>
        /// Sample object identifier used in tests.
        /// </summary>
        public const string ObjectId = "593b2662-5af7-4a90-a9cb-5a9de615b82f";

        /// <summary>
        /// Family of client IDs identifier.
        /// </summary>
        public const string FamilyId = "1";

        /// <summary>
        /// Unique identifier used in sample tokens.
        /// </summary>
        public const string UniqueId = "unique_id";

        /// <summary>
        /// Sample identity provider name.
        /// </summary>
        public const string IdentityProvider = "my-idp";

        /// <summary>
        /// Sample display name used in tests.
        /// </summary>
        public const string Name = "First Last";

        /// <summary>
        /// Sample managed identity resource identifier.
        /// </summary>
        public const string MiResourceId = "/subscriptions/ffa4aaa2-4444-4444-5555-e3ccedd3d046/resourcegroups/UAMI_group/providers/Microsoft.ManagedIdentityClient/userAssignedIdentities/UAMI";

        /// <summary>
        /// Sample virtual machine identifier.
        /// </summary>
        public const string VmId = "test-vm-id";

        /// <summary>
        /// Sample virtual machine scale set identifier.
        /// </summary>
        public const string VmssId = "test-vmss-id";

        /// <summary>
        /// Fake mTLS authentication endpoint used in tests.
        /// </summary>
        public const string MtlsAuthenticationEndpoint = "http://fake_mtls_authentication_endpoint";

        /// <summary>
        /// Sample claims JSON payload.
        /// </summary>
        public const string Claims = @"{""userinfo"":{""given_name"":{""essential"":true},""nickname"":null,""email"":{""essential"":true},""email_verified"":{""essential"":true},""picture"":null,""http://example.info/claims/groups"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";

        /// <summary>
        /// Gets the client capabilities used in tests.
        /// </summary>
        public static readonly string[] s_clientCapabilities = new[] { "cp1", "cp2" };

        /// <summary>
        /// Client capabilities encoded as claims JSON.
        /// </summary>
        public const string ClientCapabilitiesJson = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]}}}";

        // this a JSON merge from Claims and ClientCapabilitiesJson

        /// <summary>
        /// Combined claims JSON containing both client capabilities and standard claims.
        /// </summary>
        public const string ClientCapabilitiesAndClaimsJson = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]}},""userinfo"":{""given_name"":{""essential"":true},""nickname"":null,""email"":{""essential"":true},""email_verified"":{""essential"":true},""picture"":null,""http://example.info/claims/groups"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";

        /// <summary>
        /// Claims JSON containing an access_token section.
        /// </summary>
        public const string ClaimsWithAccessToken = @"{""access_token"":{""nbf"":{""essential"":true, ""value"":""1701477303""}}}";

        /// <summary>
        /// Merged client capabilities and access_token claims JSON.
        /// </summary>
        public const string ClientCapabilitiesAndClaimsJsonWithAccessToken = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""}}}";

        /// <summary>
        /// Empty claims JSON object.
        /// </summary>
        public const string EmptyClaimsJson = @"{}";

        /// <summary>
        /// Claims JSON containing an additional nested claim.
        /// </summary>
        public const string ClaimsWithAdditionalClaim = @"{""access_token"":{""nbf"":{""essential"":true, ""value"":""1701477303""},""additional_claim"":{""key"":""value""}}}";

        /// <summary>
        /// Expected merged JSON for client capabilities and an additional claim.
        /// </summary>
        public const string MergedJsonWithAdditionalClaim = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""}}}";

        /// <summary>
        /// Claims JSON with additional keys both inside and outside access_token.
        /// </summary>
        public const string ClaimWithAdditionalKey = @"{""access_token"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";

        /// <summary>
        /// Expected merged JSON for <see cref="ClaimWithAdditionalKey"/>.
        /// </summary>
        public const string MergedJsonWithAdditionalKey = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";

        /// <summary>
        /// Claims JSON with access_token appearing after another top-level key.
        /// </summary>
        public const string ClaimWithAdditionalKeyAndAccessKey = @"{""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""access_token"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";

        /// <summary>
        /// Expected merged JSON for <see cref="ClaimWithAdditionalKeyAndAccessKey"/>.
        /// </summary>
        public const string MergedJsonClaimWithAdditionalKeyAndAccessKey = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";

        /// <summary>
        /// Base64-encoded claims challenge used in tests.
        /// </summary>
        public const string ClaimsChallenge = "eyJhY2Nlc3NfdG9rZW4iOnsiYWNycyI6eyJlc3NlbnRpYWwiOnRydWUsInZhbHVlIjoiY3AxIn19fQ==";

        /// <summary>
        /// Sample displayable identifier.
        /// </summary>
        public const string DisplayableId = "displayable@id.com";

        /// <summary>
        /// Redirect URI used in OAuth tests.
        /// </summary>
        public const string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// Mobile default redirect URI used by sample mobile tests.
        /// </summary>
        public const string MobileDefaultRedirectUri = "msal4a1aa1d5-c567-49d0-ad0b-cd957a47f842://auth"; // in msidentity-samples-testing tenant -> PublicClientSample

        /// <summary>
        /// Sample client secret.
        /// </summary>
        public const string ClientSecret = "client_secret";

        /// <summary>
        /// Default password used by tests.
        /// </summary>
        public const string DefaultPassword = "password";

        /// <summary>
        /// Password used for test certificates.
        /// </summary>
        public const string TestCertPassword = "passw0rd!";

        /// <summary>
        /// Authority targeting the test tenant.
        /// </summary>
        public const string AuthorityTestTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid + "/";

        /// <summary>
        /// Instance discovery path segment.
        /// </summary>
        public const string DiscoveryEndPoint = "discovery/instance";

        /// <summary>
        /// Default authorization code used by tests.
        /// </summary>
        public const string DefaultAuthorizationCode = "DefaultAuthorizationCode";

        /// <summary>
        /// Default access token value used by tests.
        /// </summary>
        public const string DefaultAccessToken = "DefaultAccessToken";

        /// <summary>
        /// Default client assertion used by tests.
        /// </summary>
        public const string DefaultClientAssertion = "DefaultClientAssertion";

        /// <summary>
        /// Raw client ID payload encoded in base64.
        /// </summary>
        public const string RawClientId = "eyJ1aWQiOiJteS11aWQiLCJ1dGlkIjoibXktdXRpZCJ9";

        /// <summary>
        /// Header name for client SKU.
        /// </summary>
        public const string XClientSku = "x-client-SKU";

        /// <summary>
        /// Header name for client version.
        /// </summary>
        public const string XClientVer = "x-client-Ver";

        /// <summary>
        /// Token subject type value for user subjects.
        /// </summary>
        internal const TokenSubjectType TokenSubjectTypeUser = 0;

        /// <summary>
        /// Generic test message.
        /// </summary>
        public const string TestMessage = "test message";

        /// <summary>
        /// Login hint test value.
        /// </summary>
        public const string LoginHint = "loginHint";

        /// <summary>
        /// Login hint query parameter name.
        /// </summary>
        public const string LoginHintParam = "login_hint";

        /// <summary>
        /// Prompt query parameter name.
        /// </summary>
        public const string PromptParam = "prompt";

        /// <summary>
        /// Local account identifier used in tests.
        /// </summary>
        public const string LocalAccountId = "test_local_account_id";

        /// <summary>
        /// Sample given name.
        /// </summary>
        public const string GivenName = "Joe";

        /// <summary>
        /// Sample family name.
        /// </summary>
        public const string FamilyName = "Doe";

        /// <summary>
        /// Sample username.
        /// </summary>
        public const string Username = "joe@localhost.com";

        /// <summary>
        /// Sample email address.
        /// </summary>
        public const string Email = "joe@contoso.com";

        /// <summary>
        /// Sample PKeyAuth response header value.
        /// </summary>
        public const string PKeyAuthResponse = "PKeyAuth Context=\"context\",Version=\"1.0\"";

        /// <summary>
        /// Environment variable name for region discovery.
        /// </summary>
        public const string RegionName = "REGION_NAME";

        /// <summary>
        /// Valid region used in tests.
        /// </summary>
        public const string Region = "centralus"; // TODO: Tenant Migration - Update for new tenant (id4slab1) is in eastus2

        /// <summary>
        /// Invalid region used in negative tests.
        /// </summary>
        public const string InvalidRegion = "invalidregion";

        /// <summary>
        /// Default timeout in milliseconds for test operations.
        /// </summary>
        public const int TimeoutInMs = 2000;

        /// <summary>
        /// IMDS host address.
        /// </summary>
        public const string ImdsHost = "169.254.169.254";

        /// <summary>
        /// IMDS region discovery URL.
        /// </summary>
        public const string ImdsUrl = $"http://{ImdsHost}/metadata/instance/compute/location";

        /// <summary>
        /// App Service MSI endpoint used in tests.
        /// </summary>
        public const string AppServiceEndpoint = "http://127.0.0.1:41564/msi/token";

        /// <summary>
        /// Azure Arc MSI endpoint used in tests.
        /// </summary>
        public const string AzureArcEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";

        /// <summary>
        /// Cloud Shell MSI endpoint used in tests.
        /// </summary>
        public const string CloudShellEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";

        /// <summary>
        /// IMDS token endpoint used in tests.
        /// </summary>
        public const string ImdsEndpoint = $"http://{ImdsHost}/metadata/identity/oauth2/token";

        /// <summary>
        /// Machine Learning MSI endpoint used in tests.
        /// </summary>
        public const string MachineLearningEndpoint = "http://localhost:7071/msi/token";

        /// <summary>
        /// Service Fabric MSI endpoint used in tests.
        /// </summary>
        public const string ServiceFabricEndpoint = "https://localhost:2377/metadata/identity/oauth2/token";

        /// <summary>
        /// Sample user assertion token.
        /// </summary>
        public const string UserAssertion = "fake_access_token";

        /// <summary>
        /// Sample PKCE code verifier.
        /// </summary>
        public const string CodeVerifier = "someCodeVerifier";

        /// <summary>
        /// Sample nonce value.
        /// </summary>
        public const string Nonce = "someNonce";

        /// <summary>
        /// Sample realm value.
        /// </summary>
        public const string Realm = "someRealm";

        /// <summary>
        /// Sample test error code.
        /// </summary>
        public const string TestErrCode = "TestErrCode";

        /// <summary>
        /// Sample iOS broker sub-error code.
        /// </summary>
        public const string IOSBrokerSuberrCode = "TestSuberrCode";

        /// <summary>
        /// Sample iOS broker error description.
        /// </summary>
        public const string IOSBrokerErrDescr = "Test Error Description";

        /// <summary>
        /// iOS broker error metadata key.
        /// </summary>
        public const string IOSBrokerErrorMetadata = "error_metadata";

        /// <summary>
        /// Sample iOS broker error metadata value.
        /// </summary>
        public const string IOSBrokerErrorMetadataValue = @"{""home_account_id"":""test_home"", ""username"" : """ + Username + @""" }";

        /// <summary>
        /// Default Microsoft Graph .default scope.
        /// </summary>
        public const string DefaultGraphScope = "https://graph.microsoft.com/.default";

        /// <summary>
        /// Bearer token type constant.
        /// </summary>
        public const string Bearer = "Bearer";

        /// <summary>
        /// PoP token type constant.
        /// </summary>
        public const string Pop = "PoP";

        /// <summary>
        /// FMI node client identifier.
        /// </summary>
        public const string FmiNodeClientId = "urn:microsoft:identity:fmi";

        /// <summary>
        /// Gets extra query parameters used in request-building tests.
        /// </summary>
        public static IDictionary<string, string> ExtraQueryParameters
        {
            get
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "extra", "qp" },
                    { "key1", "value1%20with%20encoded%20space" },
                    { "key2", "value2" }
                };
            }
        }

        /// <summary>
        /// Gets extra query parameters annotated with whether they affect cache keys.
        /// </summary>
        public static IDictionary<string, (string, bool)> ExtraQueryParametersNoAffectOnCacheKeys
        {
            get
            {
                return new Dictionary<string, (string, bool)>(StringComparer.OrdinalIgnoreCase)
                {
                    { "extra", ("qp", false) },
                    { "key1", ("value1%20with%20encoded%20space", false) },
                    { "key2", ("value2", false) }
                };
            }
        }

        /// <summary>
        /// Key Vault URI for confidential client application secret tests.
        /// </summary>
        public const string MsalCCAKeyVaultUri = "https://id4skeyvault.vault.azure.net/secrets/AzureADIdentityDivisionTestAgentSecret/";

        /// <summary>
        /// Key Vault secret name for confidential client application tests.
        /// </summary>
        public const string MsalCCAKeyVaultSecretName = "MSIDLAB4-IDLABS-APP-AzureADMyOrg-CC";

        // TODO: Tenant Migration - New secret name for id4slab1 tenant: "MSAL-APP-AzureADMultipleOrgs"

        /// <summary>
        /// Key Vault URI for on-behalf-of tests.
        /// </summary>
        public const string MsalOBOKeyVaultUri = "https://id4skeyvault.vault.azure.net/secrets/IdentityDivisionDotNetOBOServiceSecret/";

        /// <summary>
        /// Key Vault secret name for on-behalf-of tests.
        /// </summary>
        public const string MsalOBOKeyVaultSecretName = "IdentityDivisionDotNetOBOServiceSecret";

        /// <summary>
        /// Arlington Key Vault URI for OBO tests.
        /// </summary>
        public const string MsalArlingtonOBOKeyVaultUri = "https://msidlabs.vault.azure.net:443/secrets/ARLMSIDLAB1-IDLASBS-App-CC-Secret";

        /// <summary>
        /// Arlington Key Vault secret name for OBO tests.
        /// </summary>
        public const string MsalArlingtonOBOKeyVaultSecretName = "ARLMSIDLAB1-IDLASBS-App-CC-Secret";

        /// <summary>
        /// Arlington Key Vault URI for CCA tests.
        /// </summary>
        public const string MsalArlingtonCCAKeyVaultUri = "https://msidlabs.vault.azure.net:443/secrets/ARLMSIDLAB1-IDLASBS-App-CC-Secret";

        /// <summary>
        /// Arlington Key Vault secret name for CCA tests.
        /// </summary>
        public const string MsalArlingtonCCAKeyVaultSecretName = "ARLMSIDLAB1-IDLASBS-App-CC-Secret";

        /// <summary>
        /// Authority types used in tests.
        /// </summary>
        public enum AuthorityType 
        {
            /// <summary>
            /// B2C authority type.
            /// </summary>
            B2C
        };

        /// <summary>
        /// Gets the production environment aliases used in authority metadata tests.
        /// </summary>
        public static string[] s_prodEnvAliases = new[] {
                                "login.microsoftonline.com",
                                "login.windows.net",
                                "login.microsoft.com",
                                "sts.windows.net"};

        /// <summary>
        /// Gets the default user identifier created from <see cref="Uid"/> and <see cref="Utid"/>.
        /// </summary>
        public static readonly string s_userIdentifier = CreateUserIdentifier();

        /// <summary>
        /// Creates a default user identifier using <see cref="Uid"/> and <see cref="Utid"/>.
        /// </summary>
        /// <returns>A user identifier in the form "uid.utid".</returns>
        public static string CreateUserIdentifier()
        {
            // return CreateUserIdentifier(Uid, Utid);
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Uid, Utid);
        }

        /// <summary>
        /// Creates a user identifier from the provided UID and UTID.
        /// </summary>
        /// <param name="uid">The user identifier portion.</param>
        /// <param name="utid">The tenant identifier portion.</param>
        /// <returns>A user identifier in the form "uid.utid".</returns>
        public static string CreateUserIdentifier(string uid, string utid)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", uid, utid);
        }

        /// <summary>
        /// Creates a sample MSAL token response for use in tests.
        /// </summary>
        /// <param name="tenantId">Optional tenant identifier to embed in the ID token.</param>
        /// <returns>A populated <see cref="MsalTokenResponse"/> instance.</returns>
        internal static MsalTokenResponse CreateMsalTokenResponse(string tenantId = null)
        {
            return new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(UniqueId, DisplayableId, tenantId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = s_scope.AsSingleString(),
                TokenType = "Bearer"
            };
        }

        /// <summary>
        /// Creates a sample MSAL token response with a broker token source.
        /// </summary>
        /// <returns>A populated <see cref="MsalTokenResponse"/> instance.</returns>
        internal static MsalTokenResponse CreateMsalTokenResponseWithTokenSource()
        {
            return new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(UniqueId, DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = s_scope.AsSingleString(),
                TokenType = "Bearer",
                TokenSource = TokenSource.Broker
            };
        }

        /// <summary>
        /// Gets the default test user account.
        /// </summary>
        internal static readonly Account s_user = new Account(s_userIdentifier, DisplayableId, ProductionPrefNetworkEnvironment);

        /// <summary>
        /// On-premises ADFS authority.
        /// </summary>
        public const string OnPremiseAuthority = "https://fs.contoso.com/adfs/";

        /// <summary>
        /// On-premises client identifier.
        /// </summary>
        public const string OnPremiseClientId = "on_premise_client_id";

        /// <summary>
        /// On-premises unique identifier.
        /// </summary>
        public const string OnPremiseUniqueId = "on_premise_unique_id";

        /// <summary>
        /// On-premises displayable identifier.
        /// </summary>
        public const string OnPremiseDisplayableId = "displayable@contoso.com";

        /// <summary>
        /// Fabrikam displayable identifier used in tests.
        /// </summary>
        public const string FabrikamDisplayableId = "displayable@fabrikam.com";

        /// <summary>
        /// On-premises home object identifier.
        /// </summary>
        public const string OnPremiseHomeObjectId = OnPremiseUniqueId;

        /// <summary>
        /// On-premises policy name.
        /// </summary>
        public const string OnPremisePolicy = "on_premise_policy";

        /// <summary>
        /// On-premises redirect URI.
        /// </summary>
        public const string OnPremiseRedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";

        /// <summary>
        /// On-premises client secret.
        /// </summary>
        public const string OnPremiseClientSecret = "on_premise_client_secret";

        /// <summary>
        /// On-premises UID value.
        /// </summary>
        public const string OnPremiseUid = "my-OnPremise-UID";

        /// <summary>
        /// On-premises UTID value.
        /// </summary>
        public const string OnPremiseUtid = "my-OnPremise-UTID";

        /// <summary>
        /// Gets the default on-premises test user account.
        /// </summary>
        internal static readonly Account s_onPremiseUser = new Account(
            string.Format(CultureInfo.InvariantCulture, "{0}.{1}", OnPremiseUid, OnPremiseUtid), OnPremiseDisplayableId, null);

        /// <summary>
        /// Broker extra query parameters in encoded string form.
        /// </summary>
        public const string BrokerExtraQueryParameters = "extra=qp&key1=value1%20with%20encoded%20space&key2=value2";

        /// <summary>
        /// OIDC scopes expected in broker requests.
        /// </summary>
        public const string BrokerOIDCScopes = "openid offline_access profile";

        /// <summary>
        /// Broker claims payload used in tests.
        /// </summary>
        public const string BrokerClaims = "testClaims";

        /// <summary>
        /// Successful instance discovery JSON response used in metadata tests.
        /// </summary>
        public const string DiscoveryJsonResponse = @"{
                        ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
                        ""api-version"":""1.1"",
                        ""metadata"":[
                            {
                            ""preferred_network"":""login.microsoftonline.com"",
                            ""preferred_cache"":""login.windows.net"",
                            ""aliases"":[
                                ""login.microsoftonline.com"",
                                ""login.windows.net"",
                                ""login.microsoft.com"",
                                ""sts.windows.net""]},
                            {
                            ""preferred_network"":""login.partner.microsoftonline.cn"",
                            ""preferred_cache"":""login.partner.microsoftonline.cn"",
                            ""aliases"":[
                                ""login.partner.microsoftonline.cn"",
                                ""login.chinacloudapi.cn""]},
                            {
                            ""preferred_network"":""login.microsoftonline.de"",
                            ""preferred_cache"":""login.microsoftonline.de"",
                            ""aliases"":[
                                    ""login.microsoftonline.de""]},
                            {
                            ""preferred_network"":""login.microsoftonline.us"",
                            ""preferred_cache"":""login.microsoftonline.us"",
                            ""aliases"":[
                                ""login.microsoftonline.us"",
                                ""login.usgovcloudapi.net""]},
                            {
                            ""preferred_network"":""login-us.microsoftonline.com"",
                            ""preferred_cache"":""login-us.microsoftonline.com"",
                            ""aliases"":[
                                ""login-us.microsoftonline.com""]}
                        ]
                }";

        /// <summary>
        /// Failed instance discovery JSON response used in negative tests.
        /// </summary>
        public const string DiscoveryFailedResponse =
            @"{""error"":""invalid_instance"",
               ""error_description"":""AADSTS50049: Unknown or invalid instance.\r\nTrace ID: 82e709b9-f0b3-431d-99cd-f3c2ca3d4b00\r\nCorrelation ID: e7619cf4-53ea-443c-b76a-194c032e9840\r\nTimestamp: 2021-04-14 11:27:26Z"",
               ""error_codes"":[50049],
               ""timestamp"":""2021-04-14 11:27:26Z"",
               ""trace_id"":""82e709b9-f0b3-431d-99cd-f3c2ca3d4b00"",
               ""correlation_id"":""e7619cf4-53ea-443c-b76a-194c032e9840"",
               ""error_uri"":""https://login.microsoftonline.com/error?code=50049""}";

        /// <summary>
        /// Sample OAuth token response JSON.
        /// </summary>
        public const string TokenResponseJson = @"{
                                                   ""token_type"": ""Bearer"",
                                                   ""scope"": ""user_impersonation"",
                                                   ""expires_in"": ""3600"",
                                                   ""ext_expires_in"": ""3600"",
                                                   ""expires_on"": ""1566165638"",
                                                   ""not_before"": ""1566161738"",
                                                   ""resource"": ""user.read"",
                                                   ""access_token"": ""at_secret"",
                                                   ""refresh_token"": ""rt_secret"",
                                                   ""id_token"": ""idtoken."",
                                                   ""client_info"": ""eyJ1aWQiOiI2ZWVkYTNhMS1jM2I5LTRlOTItYTk0ZC05NjVhNTBjMDZkZTciLCJ1dGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3In0""
                                                }";

        /// <summary>
        /// Sample Android broker response JSON.
        /// </summary>
        public const string AndroidBrokerResponse = @"
{
      ""access_token"":""secretAt"",
      ""authority"":""https://login.microsoftonline.com/common"",
      ""cached_at"":1591193165,
      ""client_id"":""4a1aa1d5-c567-49d0-ad0b-cd957a47f842"",
      ""client_info"":""clientInfo"",
      ""environment"":""login.windows.net"",
      ""expires_on"":1591196764,
      ""ext_expires_on"":1591196764,
      ""home_account_id"":""ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462"",
      ""http_response_code"":0,
      ""id_token"":""idT"",
      ""local_account_id"":""ae821e4d-f408-451a-af82-882691148603"",
      ""scopes"":""User.Read openid offline_access profile"",
      ""success"":true,
      ""tenant_id"":""49f548d0-12b7-4169-a390-bb5304d24462"",
      ""token_type"":""Bearer"",
      ""username"":""some_user@contoso.com""
   }";

        // do not change these constants!

        /// <summary>
        /// Raw AAD client info payload used in tests.
        /// </summary>
        public const string AadRawClientInfo = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0";

        /// <summary>
        /// Raw MSA client info payload used in tests.
        /// </summary>
        public const string MsaRawClientInfo = "eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ";

        /// <summary>
        /// Raw B2C client info payload used in tests.
        /// </summary>
        public const string B2CRawClientInfo = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9";

        //Region Discovery Failures

        /// <summary>
        /// Region auto-detect failure message for HTTP 200 with empty response.
        /// </summary>
        public const string RegionAutoDetectOkFailureMessage = "Call to local IMDS failed with status code OK or an empty response.";

        /// <summary>
        /// Region auto-detect failure message for HTTP 404 or empty response.
        /// </summary>
        public const string RegionAutoDetectNotFoundFailureMessage = "Call to local IMDS failed with status code NotFound or an empty response.";

        /// <summary>
        /// Region auto-detect failure message for HTTP 500 scenarios.
        /// </summary>
        public const string RegionAutoDetectInternalServerErrorFailureMessage = "Service is unavailable to process the request";

        /// <summary>
        /// Error message used when region discovery is not supported.
        /// </summary>
        public const string RegionDiscoveryNotSupportedErrorMessage = "Region discovery can only be made if the service resides in Azure function or Azure VM";

        /// <summary>
        /// Generic IMDS failure message used in region discovery tests.
        /// </summary>
        public const string RegionDiscoveryIMDSCallFailedMessage = "IMDS call failed";

        /// <summary>
        /// Log message used when serializing cache with PII.
        /// </summary>
        public const string PiiSerializeLogMessage = "MsalExternalLogMessage: Serializing Cache Pii";

        /// <summary>
        /// Log message used when deserializing cache with PII.
        /// </summary>
        public const string PiiDeserializeLogMessage = "MsalExternalLogMessage: Deserializing Cache Pii";

        /// <summary>
        /// Log message used when serializing cache without PII.
        /// </summary>
        public const string SerializeLogMessage = "MsalExternalLogMessage: Serializing Cache without Pii";

        /// <summary>
        /// Log message used when deserializing cache without PII.
        /// </summary>
        public const string DeserializeLogMessage = "MsalExternalLogMessage: Deserializing Cache without Pii";

        /// <summary>
        /// Generic OIDC JWK response used in discovery tests.
        /// </summary>
        public const string GenericOidcJwkResponse = @"{""keys"":[{""kty"":""RSA"",""use"":""sig"",""kid"":""66682C848A3140685FC883FD7EA993CC"",""e"":""AQAB"",""n"":""pY-a5km28zOE-KS1UgYlWS9AT-4eYdxAlTVeGaSq21dhbB4L6tmlUiiV8s-Zv_L5Ng6rC1asmjEVtrKmFkYMoW4RbJC6HAzQbS7crGglyTJ39uDGJBpeQZCWYUljlIzp2VAJnPxG1-iyIDjZSOuGgvTxiphV4j2naU46RcT3IfC7CPkUZUtmqpbYNOHRli_oVirxGUMjHbq623qOCQUkUfMBLhKr0EjrZtcispSDzHqWktUO7K8Iy8D6VyttPIuzVkYx1GYiB0jCF1jgIDyEnH1E3r6S5ytao9KvoO6DGZTzFTJL2-i_uPco1DXfXFlVO9jKb5MHomO3NNrSDNRSnQ"",""alg"":""RS256""}]}";

        /// <summary>
        /// Generic OIDC discovery document used in tests.
        /// </summary>
        public const string GenericOidcResponse = @"{
   ""issuer"":""https://demo.duendesoftware.com"",
   ""jwks_uri"":""https://demo.duendesoftware.com/.well-known/openid-configuration/jwks"",
   ""authorization_endpoint"":""https://demo.duendesoftware.com/connect/authorize"",
   ""token_endpoint"":""https://demo.duendesoftware.com/connect/token"",
   ""userinfo_endpoint"":""https://demo.duendesoftware.com/connect/userinfo"",
   ""end_session_endpoint"":""https://demo.duendesoftware.com/connect/endsession"",
   ""check_session_iframe"":""https://demo.duendesoftware.com/connect/checksession"",
   ""revocation_endpoint"":""https://demo.duendesoftware.com/connect/revocation"",
   ""introspection_endpoint"":""https://demo.duendesoftware.com/connect/introspect"",
   ""device_authorization_endpoint"":""https://demo.duendesoftware.com/connect/deviceauthorization"",
   ""backchannel_authentication_endpoint"":""https://demo.duendesoftware.com/connect/ciba"",
   ""frontchannel_logout_supported"":true,
   ""frontchannel_logout_session_supported"":true,
   ""backchannel_logout_supported"":true,
   ""backchannel_logout_session_supported"":true,
   ""scopes_supported"":[
      ""openid"",
      ""profile"",
      ""email"",
      ""api"",
      ""resource1.scope1"",
      ""resource1.scope2"",
      ""resource2.scope1"",
      ""resource2.scope2"",
      ""resource3.scope1"",
      ""resource3.scope2"",
      ""scope3"",
      ""scope4"",
      ""shared.scope"",
      ""transaction"",
      ""offline_access""
   ],
   ""claims_supported"":[
      ""sub"",
      ""name"",
      ""family_name"",
      ""given_name"",
      ""middle_name"",
      ""nickname"",
      ""preferred_username"",
      ""profile"",
      ""picture"",
      ""website"",
      ""gender"",
      ""birthdate"",
      ""zoneinfo"",
      ""locale"",
      ""updated_at"",
      ""email"",
      ""email_verified""
   ],
   ""grant_types_supported"":[
      ""authorization_code"",
      ""client_credentials"",
      ""refresh_token"",
      ""implicit"",
      ""password"",
      ""urn:ietf:params:oauth:grant-type:device_code"",
      ""urn:openid:params:grant-type:ciba""
   ],
   ""response_types_supported"":[
      ""code"",
      ""token"",
      ""id_token"",
      ""id_token token"",
      ""code id_token"",
      ""code token"",
      ""code id_token token""
   ],
   ""response_modes_supported"":[
      ""form_post"",
      ""query"",
      ""fragment""
   ],
   ""token_endpoint_auth_methods_supported"":[
      ""client_secret_basic"",
      ""client_secret_post"",
      ""private_key_jwt""
   ],
   ""id_token_signing_alg_values_supported"":[
      ""RS256""
   ],
   ""subject_types_supported"":[
      ""public""
   ],
   ""code_challenge_methods_supported"":[
      ""plain"",
      ""S256""
   ],
   ""request_parameter_supported"":true,
   ""request_object_signing_alg_values_supported"":[
      ""RS256"",
      ""RS384"",
      ""RS512"",
      ""PS256"",
      ""PS384"",
      ""PS512"",
      ""ES256"",
      ""ES384"",
      ""ES512"",
      ""HS256"",
      ""HS384"",
      ""HS512""
   ],
   ""authorization_response_iss_parameter_supported"":true,
   ""backchannel_token_delivery_modes_supported"":[
      ""poll""
   ],
   ""backchannel_user_code_parameter_supported"":true
}";

        /// <summary>
        /// Gets an OIDC discovery document, optionally replacing the default authority.
        /// </summary>
        /// <param name="authority">The authority to inject into the response. If null or empty, the generic response is returned.</param>
        /// <returns>An OIDC discovery document as a JSON string.</returns>
        public static string GetOidcResponse(string authority = null)
        {
            if (string.IsNullOrEmpty(authority))
            {
                return GenericOidcResponse;
            }
            return GenericOidcResponse.Replace("https://demo.duendesoftware.com", authority.TrimEnd('/'));
        }

        /// <summary>
        /// Creates a sample AAD test token response.
        /// </summary>
        /// <returns>A deserialized <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateAadTestTokenResponse()
        {
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.\",\"client_info\":\"" + AadRawClientInfo + "\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        /// <summary>
        /// Creates a sample MSA test token response.
        /// </summary>
        /// <returns>A deserialized <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateMsaTestTokenResponse()
        {
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Tasks.Read User.Read openid profile\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJ2ZXIiOiIyLjAiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vOTE4ODA0MGQtNmM2Ny00YzViLWIxMTItMzZhMzA0YjY2ZGFkL3YyLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwiYXVkIjoiYjZjNjlhMzctZGY5Ni00ZGIwLTkwODgtMmFiOTZlMWQ4MjE1IiwiZXhwIjoxNTM4ODg1MjU0LCJpYXQiOjE1Mzg3OTg1NTQsIm5iZiI6MTUzODc5ODU1NCwibmFtZSI6IlRlc3QgVXNlcm5hbWUiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtc2Fsc2RrdGVzdEBvdXRsb29rLmNvbSIsIm9pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInRpZCI6IjkxODgwNDBkLTZjNjctNGM1Yi1iMTEyLTM2YTMwNGI2NmRhZCIsImFpbyI6IkRXZ0tubCFFc2ZWa1NVOGpGVmJ4TTZQaFphUjJFeVhzTUJ5bVJHU1h2UkV1NGkqRm1CVTFSQmw1aEh2TnZvR1NHbHFkQkpGeG5kQXNBNipaM3FaQnIwYzl2YUlSd1VwZUlDVipTWFpqdzghQiIsImFsZyI6IkhTMjU2In0.\",\"client_info\":\"" + MsaRawClientInfo + "\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        /// <summary>
        /// Creates a sample B2C test token response.
        /// </summary>
        /// <returns>A deserialized <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateB2CTestTokenResponse()
        {
            const string jsonResponse = "{\"access_token\":\"<removed_at>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIn0.\",\"token_type\":\"Bearer\",\"not_before\":1538801260,\"expires_in\":3600,\"ext_expires_in\":262800,\"expires_on\":1538804860,\"resource\":\"14df2240-96cc-4f42-a133-ef0807492869\",\"client_info\":\"" + B2CRawClientInfo + "\",\"scope\":\"https://iosmsalb2c.onmicrosoft.com/webapitest/user.read\",\"refresh_token\":\"<removed_rt>\",\"refresh_token_expires_in\":1209600}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        /// <summary>
        /// Creates a sample B2C test token response that includes a tenant identifier.
        /// </summary>
        /// <returns>A deserialized <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateB2CTestTokenResponseWithTenantId()
        {
            const string jsonResponse = "{\"access_token\":\"<removed_at>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIiwidGlkIjoiYmE2YzBkOTQtYThkYS00NWIyLTgzYWUtMzM4NzFmOWMyZGQ4IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20ifQ.\",\"token_type\":\"Bearer\",\"not_before\":1538801260,\"expires_in\":3600,\"ext_expires_in\":262800,\"expires_on\":1538804860,\"resource\":\"14df2240-96cc-4f42-a133-ef0807492869\",\"client_info\":\"" + B2CRawClientInfo + "\",\"scope\":\"https://iosmsalb2c.onmicrosoft.com/webapitest/user.read\",\"refresh_token\":\"<removed_rt>\",\"refresh_token_expires_in\":1209600}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        /// <summary>
        /// Creates a sample AAD token response that includes a family ID.
        /// </summary>
        /// <returns>A deserialized <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateAadTestTokenResponseWithFoci()
        {
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.\",\"client_info\":\"" + AadRawClientInfo + "\",\"foci\":\"1\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        /// <summary>
        /// Creates a sample AAD token response for the MSAL User Default account.
        /// </summary>
        /// <returns>A deserialized <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateAadTestTokenResponseWithMsalUserDefault()
        {
            // Token response with MSAL User Default user information for ID4SLAB1 tenant
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiTVNBTCBVc2VyIERlZmF1bHQiLCJvaWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJNU0FMLVVzZXItRGVmYXVsdEBpZDRzbGFiMS5vbm1pY3Jvc29mdC5jb20iLCJzdWIiOiJZNllrQmRITk5MSE5tVEtlbDlLaFJ6OHdyYXN4ZExSRmlQMTRCUlBXcm40IiwidGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIiwidXRpIjoiNm5jaVgwMlNNa2k5azczLUYxc1pBQSIsInZlciI6IjIuMCJ9.\",\"client_info\":\"" + AadRawClientInfo + "\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        // Fake strings approximately representing tokens of real-world size

        /// <summary>
        /// Sample app access token string sized to resemble a real-world token.
        /// </summary>
        internal const string AppAccessToken = "a9sVdA2UrU0KbsG19MjSZp4hlrd4B3mhZc1gfWsu1v3Nxuf9y7MMqdmCqyt4OwJ7cZEFAhdy8wf9vRastaS0Dcc6MnAtaI73k9co06QtQgecLPVMR23ht6cWB25Ta7yqdcQ8X7ARdUD4MWY6o01TqADKE4Vo3DMpzZiwpiS6Z81I5bW9jbWiUTT7J44lRM1qR3ZJUdaa6OSOaLZT4temLYy3bXZh6Bvu7fxOF1pStzwESRDiFegYq6LFf1sHVY5scdvNOifQnWuRy4bWw0Fl17IO5lKzhuEUvaOcgyWea0Hg3Rh9U0or2WPogJna7HraHdp0BWtuj3KOdXqmjxDoFKkTfBXiubpnAqlWwfAKHvKYefNuSRT7ewHLFQNpVVbr93P1Na4aMHqL4DWIQ1BEYoRskQkxNDtzeipb5CP5FrfStz8lFB4nCaZsJPqIzi82BpZ4HwXls8VYrRLt2dK0D9ksxufhN2uUknO2w5MBDvpwl5IuPZiCvwxU0IG3eDyqRCqw1uh18KL4qbeAwp0BPQL7Xexc4eCXC5o24uxipyWK6C169R645sYCgHY9Phiik9fUaTcZ6rzPhSKXK5svhtuMUpDHoHnSkOocCmJPRdNEpULup7zAMtGxGtD4ldfRRtAD5MLBcnKB4QN4KsbysV9lj1xDSFawL22pno61hreo5lJmqlRdj3yRVupqg5IRhilWI0wbUSfUPISrBS0EcCb6rSBd6MPIbstJYOsuJ4Bh4wtLDDeD5oxlMIaF2tkf2QHfspXocd0fLbpA7X9AR1s5yYRRJKgh7SJMgS4JJxLHvF9VZJCKKwnYzlR5EaHYsvDCGyvEi9UkWDCBYMcG6OvzYemjVOTiJTbNM2q6DaqLS9bOMZdZzNfG3PlMAqeZlH5CLv8edLzfDaVZmMCdsc6iUTCAgW4ERDzu7Pf2AeQHQlpplPkgYMxiKZ31BxoaHvwlGReopWq3NN8oOS9QG5VCG1osQRmBZAQHDZTA2x30C4l8L4yH8tvs12trqAvRGnFIq79qluVwJHAPGbQZiAEArzpN7laNqDmzJFCA4emv6DdnCnGdLCSaIubVTBJ5dXbBN9xmphJUGw1jCoFDcFxrcOK0MdJnVGnbkEuSrnEVHUGmocUG1tb0QRZ9Jhn9mGO6RxVcZVzs7XJi8vEQ0FeWJOG0uqsNyi0gCmsglyH8QqsOVlYve8UuxqCgzm1CgfCeNDJEB01xk6iTsjRM0iGcCaZGCm9dRdW99Hlq8SsLRZl2IT48KYj4B9ZP4jBrkY9Ef3xpFRu8fiwgBdzrZosZA2aQPRXlvdM1XjeGaK7iUsrpsVOrvDorBoMucV3uypI85c7yQaTu1qrKfri7OZgEVVBdQO08iipyvyRVSBz1U9ZAc0xqgERqPUoPIOFtqMRx1qJD7WWRUa0hnd66SAgiM2ViGBZspIsUvA9Kn";

        /// <summary>
        /// Sample user access token string sized to resemble a real-world token.
        /// </summary>
        internal const string UserAccessToken = "flMpQIKiCoiPK6qISSjmF9dGhKe47KFGPwe82BDBxBCVfYI4UiKYbBuShsjf8oGTsjN5ODeaO6k0cmZJYuNNbLyOr8JGqoxQRW9bI8j5ETpbTNf6tYpAWde9PIYj2wEBnbughVgtJsh2QxIrahie5leMpsGb1yoFzADD5gyoJq8etNUSgZwe5qkfaE9UBCUKrznKjKbsG5hBJXut5GD0QdQy3wo2PnocewrptlMzd5SsHCzUUBGA4q7ks7IfrLiQH11JyBnjBhypOX3XvuqBz4JKkpftVYfvwPWE3f5Onku6FkZJFFESyGQP9YnJVx5dQCpHH9l6ShTqOLSQduf7wxoyeAgxwPrM9Y8Kvj31IrXqiwP52x4hBsctLCqOXOZ3wMXnozMXyHpNvKMJaNgDgvBgMYhiyORkb3qKYw0gAP4659I8dK1esxJoD8I3EreDftGfNMFCgn7kFfauUQphkqx8ukqzw068R7g5TOUci1pgPcVXCAMxj0P3fTiKe1doVuF6znKYh3m7pjyzyaqb5K9VFIh4A8TXOO0MqjaVkoSWJXARTy4T0kAZBVPbO6U2BWku23yLIt43MhQTc9uf7inuirwaIgh5u7noDxYG4QZLB1CJl04Zq2gbh9GW7dqweAaC9efYTEDwhxDTPHeGTQs44e8cnWerIyZA7mq8sFuzihIiCfgZ6nNBPcx2lXKyarUtQGmjjRyOEAhs66atv3SgMhNBhontPoUhR1QEnTKeYzfaavlnf5qMZA41hijGazHyxy5FgLD5aLEpZTHN5MPQLeaEXzDMX5Wtdvq7nokiItRfLkKZtXkuSiFVltmRPcKqzGbjNRH96OQzuxLE1Mv25FYFR3PAwv6np69yScVOpNFL8CqJdT310dGnRPUKSrEqTPuMsHqVRr36j2ZUaGs6YBtcrxIxKHuPrv23FQg5fC0FgxZvKqve0hf68AocJ1HqKRy01CGQobmYpTwBByftOZYGC4KOfGd13l78kZaKLuk2gxfFuTQyr11A0L4n5tXfjlikJtr3wlTGt0KCGGXmNK1xsSoRC0VcXDOgQUu3FHblhiaYjbSvPRF09xn9tRPnUkznbsT1kPMiJ8v89ZOCtVWpvkoiy9VUVcSUpZNQwRh3wHidZAkp1xyjyVc2pIHPg6XhzJnlt77zHNiBkPxWbYt7hXBQf3QeYoMF4s0Qi1y5N72DdoSNJ3iaTwx3esAz6TeyxSh36PIz35mR5jGyGMssyaNg6lIewLPbjnizgC6xssi6mKOheDqWqBv89nIvSBOXEkKcUYsBlhBBK6BgxOIha1NAeP93RRKfyjrF7LtIoSOk3DJUx75rUJ9oyuuTt4FdSnp7ZdrIciO8vlNslPrfa7UjBdOtVHiaz9Ef91dctdADVFcwXXmcu2ypyKB1YvMbkPP7mc12TF1a8X6t0mU4s4J4IpA3SHmT5JvbQBEzOIs6ex38X3UtXSItxpaS2gKozAhAmvjt6NKMe3Jysm4bafH1kb8eB1vdwTQu3jIOGozqHC3rvqEVAt26NNKOuNYAoYYamQOSb2w8PUCuDDWs1ffLvvfyvRndZztV5C4HGGR1Tg82N291Sb7rSUYmA1rdGyJ4kPtSaiPOwMyPUs9FuZNef5Ib83D3gTcgS1gMxto5UkfSxtCDKLXtGKArOdACrRzHiiMSn3owQfyVtSXZPdeofoCzuPWcZzFLBUJR0iKWBpUkxd0N17vw45uMQpQUNGgGoyvyboKkAFlOGsEIAmrnooC3CJGVA4jHPYJnVG4xTJ37U6QL5sX95qWtjbvuD5KoT2GyWec0o62CNr09tCQsiALLC1QrfCiCGsullefbsgBB5tsOY1Kyiy4uf84qBMu20GbsJ01R8xxpJ5bh6HFRaStEK3WIy7TMJym42YMbxB3AGsGFGhNYljtuqgeUjXn1UuWskkB6QqdepFHCof6CHg0LlV0o4Iz9QKu5cfoi8jk5HKbvIGyDqCgZaC2LdugNgQ0X";

        /// <summary>
        /// Sample refresh token string sized to resemble a real-world token.
        /// </summary>
        internal const string RefreshToken = "mhJDJ8wtjA3KxpRtuPAreZnMcJ2yKC2JUbpOGbRTdOCImLyQ2B4EIhv8AiA2cCEylZZfZsOsZrNsMBZZAAU9TQYYEO72QcdfnIWpAOeKkud5W2L8nMq6i9dx1EVIl09zFXhOJ79BdFbU0Eb5aUHlcqPCQjec62UKBLkZJmtMnoAa8cjvgIuxTdVM8FNdghe5nlCNTEVooKleTTEHNl2BrdyitLaWTKSP0lRqnFxriG0xWcJoSMsdS7Vt6HZd1TkwHIXycNMlCcCdUh5tOgqx1M8y8uoXK4OJ1LQmtkZvcQWcycvOCPACYakKM1pUQqwTxI6Y4HrL38sqQaSNxpF9OcFxOQWpuGodRekCbxXVbWclttIpvSOLaBhZ2ZBpcCBEeEMSmhqqYgajNwwwe9w88u0UsYKe6PBbaI48ENr02u2qBeLsIQ2HUyKlN3iVmX7u7MhgDWA3NNavMtlLmWd63NfuDgXpLI0O4cLhjAx8uoBIK8LntXPHPTxJ28o0yrszvD4gf7RdhuTq5VE15zne6iAJgIGfy7latGFzxuDMcML9OoXURHnNEHBgS9ZQCfNzYZ2O9flF1UjGpcBLEi7hHVHnrQb4y7c98dz9p62cvEMhorGx9kCwSIkOae5LheXPQkFIbsGyomNEwz3HZvR131VGAwdfmUUodvPr6LAAtmjl4sZ72PRqAo8EdQ0IFsWoypXVv51IooR87tO3uiG2DkxhIAwumOQdaJNxw1a0WS9mpQOmwFlvfbZkaIoUKgagHc8fVa1aHZntLGwH0S1iYixJiIrMnPYAeRdSp9mlHllrMX8xUIznobcZ5i8MpUYCKlUXMZ82S3XUJ5dJxARNRPxXlLJ5LPYBhUNkBLQen9Qmq3VZEV1RDJyhbGp6GAo14KsMtVAVYNmYPIgo85pCZgOwVEOBUycszu4AD3p4PT2ella4LVoqmTTMSA5GEWoeWb5JvEo222Z0oKr7UK8dGwpWRSbg8TNeODihJaTUDfErvbgaZnjIRpqfgtM5i1HfQbD7Yyft5PqyygUra7GYy7pjRrEvq95XQD8sAZ32ku9AqCo5qOB584iX881WErOoheQZokt1txqwuIMUyhVuMKNEXy70CeNTsb30ghQMZpZcXIkrLYyQCZ0gNmARhMKagCSdrpUtxudLk44yfmuwSQzBN3ifWfLZiFpU53qdPLZoTw5";

        /// <summary>
        /// Sample ID token string sized to resemble a real-world token.
        /// </summary>
        internal const string IdToken = "6GwdM7f6hHXfivavPozhaRqrbxvEysfXSMQyEKBwVgivPZTtmowsmYygchhIuxjeFFeq1ZPHjhxKFnulrvoY6TDerZY5xyOlg45bToI9Bu95qFvUrrt5r17UJcXdw4YkvEt10CcDDcLcEYw704RpVefvbpjbF24pOgIuafcAkDnbDA0Qea4ePuSC45Lw7zpJhbo9Gh8IfMX597fayBvMs3fh7frrm9KpWMCeKY3h99YSaCYjZFKp1ppvXXPE9bc4sh4pRDOfnv0Yr9J8u4elZevEE4qGddfgd3hYb18XPGRjPEMlWsh7tnwxwUm6OSZlMTHYuvwBENNMx7SUQmMeg4rCfgnbcNDkWpXCiSDVt1lLLv8F2GjYnM6De3v1Ks5lhBWx3grLggcN9LnXz92eJ1l5lTB2v0y9MgmFZ4gY43oIOW5n8G5HOx3bGOyjTw0TKKbyVa3mDj0A3QqW8eLTUJz42BNiGOf5m9prMSlpAW59CHCMJLatsj3IvGeCITsGAr3sUZEytORWUdxCfuIPwecQgU6bO7pNqNvZc1tJHHNwJlfS23ZkiFuEXqEThHYfxBCFxAzMDlzO0TOdWhvrb8hlNeAOcNhoAKxu7HXsePajKs4fU1rcdSxzNKwtASEla3p6jfJnnDtKf38RJZPaRRYMviqqWEMhjmqIvBm7sMaf8RyNNuYl7otZwmwNVCR1hzzmaTAy4kQce67FJqFba7uizrgwp9zsvK8muCHKKPvNthy7fHsxKmrBIm0bLcoePKK3wAID4kFvNQcxXp6rAOr8bLFF3bLEoYdzmF2QJz1frVZZHHPy90Cmlhw48EQN8NE2OllpdaykKt5k4rPcZQyitayNNhism30qh7eCBhcA7mm5Ja0S8X4VPlkwvgwg0mQuul6gakmja8xpnTrwiOdtao320GDmJaJA6zf3UTpNZTq9tdfBtUrjAD8RS0tNUBT3Ko8N2Lfh9ry8y9ESmRVIhch3rKY7UeefFAnkiwH2WwC57ZEsHtMP0SwKYtYKHZW9HkERCCyqOT1Mw0IavsLGFvchzMAvTnz4RwRBk6IrWgANvqT3F3Vexc2K0poKb71XZ4aMXxjqAzydGQAKpKJEJcqEvX9RD8nL76TF2LZIepiaZ3dbQImkqSjbF7aaY2JFoN9ZWlcSQKe8zdO8TIG16bF8W9R4ldDyzV39L33KcweG";

        #region Test Certificate and Private Key (ExpiredRawCertificate, ValidRawCertificate & XmlPrivateKey)
        /// <summary>
        /// Test (expired and valid) PEM-encoded X.509 certificate and their matching RSA private key.
        /// These are used together in unit tests that require both a certificate and its private key.
        /// The <see cref="ExpiredRawCertificate"/>/<see cref="ValidRawCertificate"/> and <see cref="XmlPrivateKey"/> are a matched pair:
        /// - <see cref="ExpiredRawCertificate"/> is an expired PEM-encoded certificate. The certificate is valid for 1 day and was created on September 8 2025, ensuring it will always be expired.
        /// - <see cref="XmlPrivateKey"/> is their corresponding RSA private key in XML format.
        /// </summary>
        internal const string ExpiredRawCertificate = "MIIC/zCCAeegAwIBAgIUGSVU23Wc0+QtCbUTjsyPOrc0XpEwDQYJKoZIhvcNAQELBQAwDzENMAsGA1UEAwwEVGVzdDAeFw0yNTA5MDgyMjAxMTdaFw0yNTA5MDkyMjAxMTdaMA8xDTALBgNVBAMMBFRlc3QwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC5XNEuk3cIEChkZd2P/bljUaVqNVh4mbXdWHYAgbdK48U6rG0FLq1NAfSnZO0EPbK8Zo4psRh2lBcqW29/WsKiHUEHLkLyFI+frEIfc8wskd+WxkKfL8G52uRpYQCG87FIv8uZBBlDG7kDdOV36CUkK1N+V2fHbkEgx+YfWg6+pLi3KQx6Pf/b2YqLD36hj8WRrVYzL6yXVUBiyRd+cQ9y5V/MRtoiX1Sv8WEFYtzIG0TUGi9pR7WWhgHNQk6DFDzutMV62ZEBNPIQvdO2EwXGr1FUIOL6zmj6bArPhY+hCXGrAAwCXodZhgZ95BxTwsQWtjCha2hT6ed8zmoE72FdAgMBAAGjUzBRMB0GA1UdDgQWBBQPYq0Efzuv1diVcgxBxTnVA4wLMjAfBgNVHSMEGDAWgBQPYq0Efzuv1diVcgxBxTnVA4wLMjAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQCXAD7cjWmmTqP0NX4MqwO0AHtO+KGVtfxF8aI21Ty/nHh2SAODzsemP3NBBvoEvllwtcVyutPqvUiAflMLNbp0ucTu+aWE14s1V9Bnt6++5g7gtXItsNV3F/ymYKsyfhDvJbWCOv5qYeJMQ+jtODHN9qnATODT5voULTwEVSYQXtutwRxR8e70Cvok+F+4I6Ni49DJ8DmcYzvB94uthqpDsygY1vYzpRbB5hpW0/D7kgVVWyWoOWiE1mV7Fry7tUWQw7EqnX89kMLMy4g6UfOv4gtam8RBa9dLyMW1rCHRxOulP47joI10g9JoJ9DssiQTUojJgQXOSBBXdD20H+zl";
        internal const string ValidRawCertificate = "MIIDATCCAemgAwIBAgIUSfjghyQB4FIS41rWfNcZHTLE/R4wDQYJKoZIhvcNAQELBQAwDzENMAsGA1UEAwwEVGVzdDAgFw0yNTA4MjgyMDIxMDBaGA8yMTI1MDgwNDIwMjEwMFowDzENMAsGA1UEAwwEVGVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALlc0S6TdwgQKGRl3Y/9uWNRpWo1WHiZtd1YdgCBt0rjxTqsbQUurU0B9Kdk7QQ9srxmjimxGHaUFypbb39awqIdQQcuQvIUj5+sQh9zzCyR35bGQp8vwbna5GlhAIbzsUi/y5kEGUMbuQN05XfoJSQrU35XZ8duQSDH5h9aDr6kuLcpDHo9/9vZiosPfqGPxZGtVjMvrJdVQGLJF35xD3LlX8xG2iJfVK/xYQVi3MgbRNQaL2lHtZaGAc1CToMUPO60xXrZkQE08hC907YTBcavUVQg4vrOaPpsCs+Fj6EJcasADAJeh1mGBn3kHFPCxBa2MKFraFPp53zOagTvYV0CAwEAAaNTMFEwHQYDVR0OBBYEFA9irQR/O6/V2JVyDEHFOdUDjAsyMB8GA1UdIwQYMBaAFA9irQR/O6/V2JVyDEHFOdUDjAsyMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEBAAOxtgYjtkUDVvWzq/lkjLTdcLjPvmH0hF34A3uvX4zcjmqF845lfvszTuhc1mx5J6YLEzKfr4TrO3D3g2BnDLvhupok0wEmJ9yVwbt1laim7zP09gZqnUqYM9hYKDhwgLZAaG3zGNocxDEAU7jazMGOGF7TweB7LdNuVI6CqgDOBQ8Cy2ObuZvzCI5Y7f+HucXpiJOu1xNa2ZZpMpQycYEvi5TD+CL5CBv2fcKQRn/+u5B3ZXCD2C9jT/RZ7rH46mIG7nC7dS4J2o4JjmlJIUAe2U6tRay5GvEmc/nZK8hd9y4BICzrykp9ENAoy9i+uaE1GGWeNgO+irrcrAcLwto=";
        internal const string XmlPrivateKey = @"<RSAKeyValue>
  <Modulus>uVzRLpN3CBAoZGXdj/25Y1GlajVYeJm13Vh2AIG3SuPFOqxtBS6tTQH0p2TtBD2yvGaOKbEYdpQXKltvf1rCoh1BBy5C8hSPn6xCH3PMLJHflsZCny/BudrkaWEAhvOxSL/LmQQZQxu5A3Tld+glJCtTfldnx25BIMfmH1oOvqS4tykMej3/29mKiw9+oY/Fka1WMy+sl1VAYskXfnEPcuVfzEbaIl9Ur/FhBWLcyBtE1BovaUe1loYBzUJOgxQ87rTFetmRATTyEL3TthMFxq9RVCDi+s5o+mwKz4WPoQlxqwAMAl6HWYYGfeQcU8LEFrYwoWtoU+nnfM5qBO9hXQ==</Modulus>
  <Exponent>AQAB</Exponent>
  <P>3pGBJXfhILNTsbRLHmUy7YVvD75HpvMCey2aaN4gU9Jvi1s2vQFU15a8p75Yt8UYHZDr+Yqwl1Jd4J+UtWsGqGBGNB1Ae4V1dwR8zUDKxXXee7G/dCDnIu4xpkZbPD+brcULcpF/Tdq/WsTbpCNhPgjHuo8hQY3vFv1NMla8mr0=</P>
  <Q>1TSgE9DfTeqk0qybQM1r83M5ZwWKV0mPQBZl1VMs+VplB6E/6JAYWCKiq9ewgocOaktK94jtEtsaDhYeyojZFBlukt1lKp4kmkUwUSEmi3EFsprNakg+Bm6t85tEm5he5mG1ivHlE3M5lBWJ2A0r1g3jWSjYJlkk2nOwFE8bmyE=</Q>
  <DP>UIcU0xmsusgnYAR7qWO0KXw90tRl2GHUY/z8ATVdPPbGpQU7qObya45+c7LLJrKJJyloN8GWYynKDZuvknRG1GUBAZoT2p1PAuD8xsbKlucuuFJ3kuzUtC66iA6ss//Ps++3VJyQEvsygQT480pZxLgoi7d9sNpJx2eeprf7RYE=</DP>
  <DQ>zwIZqyPSrUR2ZFdTJshNWEM4KN8oQzgY7pDQrx/jOviZv57A/n1qJaj7aP4zU4juZiZU06MPDI/P7H1tyBi3LNzEj7SG1apWv7MOBre5RQqoDZJggCFEl9o+65iGNMzs16NnMVFMqmXmMfH3tN6VAXDanWca96D2N2S8QfvNQgE=</DQ>
  <InverseQ>Uoxh1dskd3C0N7SQ1nJXW7FyjB+J54R5yAcd8Zk0ukunhtuzsziQH4ZoMhBuzwxRwOaw0Umj77EcdEevuvFHn6LAK/solK2lkRcuKY2QTgkbYyYOxZNa1pJJaAfgzSGsBiwiGtHXl2eFLb2jfYDa4V/SV2B6BPOVheSUQGZlyYM=</InverseQ>
  <D>Lkq21wnu7S2T2NbzyVUVKm+mfurJqHzCxX+lIKVEkEhn5ipPo76vew7k+bUj2C5MZ+64zEK1GFANpP9mzghtmSzzI4bzIx/tanQLo2047VyU2UO0Oaskl3TKHGMkTY+ok8GKaDF02aSfxPQ5poNsWycS1/eeLFklnLkviF7mVcfCoStSHAb+8dQzxO22Mu+oN2rXHinoNDSmFzUTx8cJapQhgji+GADRKF77Sfa5tHk/hCzVUXGBHgBs1jJM9cin2BBij8PngOaAAlby4gr07/r8SZU2uuXoxEDhpxf6mRTET5Wr2hxAyhu3bpZeCc0LokckNkzJPGUG6JaXXdUcgQ==</D>
</RSAKeyValue>";
        #endregion
    }

    /// <summary>
    /// Contains constants used by tests targeting the ADFS 2019 lab environment.
    /// </summary>
    internal static class Adfs2019LabConstants
    {
        /// <summary>
        /// ADFS authority for the 2019 lab.
        /// </summary>
        public const string Authority = "https://fs.msidlab8.com/adfs";

        /// <summary>
        /// App identifier used in the ADFS 2019 lab.
        /// </summary>
        public const string AppId = "TestAppIdentifier";

        /// <summary>
        /// Public client identifier used in the ADFS 2019 lab.
        /// </summary>
        public const string PublicClientId = "PublicClientId";

        /// <summary>
        /// Confidential client identifier used in the ADFS 2019 lab.
        /// </summary>
        public const string ConfidentialClientId = "ConfidentialClientId";

        /// <summary>
        /// Redirect URI used by ADFS 2019 lab client tests.
        /// </summary>
        public const string ClientRedirectUri = "http://localhost:8080";

        /// <summary>
        /// Gets the supported scopes for the ADFS 2019 lab.
        /// </summary>
        public static readonly SortedSet<string> s_supportedScopes = new SortedSet<string>(new[] { "openid", "email", "profile" }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Key Vault URI for the ADFS 2019 client secret.
        /// </summary>
        public const string ADFS2019ClientSecretURL = "https://id4skeyvault.vault.azure.net/secrets/ADFS2019ClientCredSecret/";

        /// <summary>
        /// Key Vault secret name for the ADFS 2019 client secret.
        /// </summary>
        public const string ADFS2019ClientSecretName = "ADFS2019ClientCredSecret";
    }
}
