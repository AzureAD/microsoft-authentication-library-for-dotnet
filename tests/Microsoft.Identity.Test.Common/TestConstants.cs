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
    internal static class TestConstants
    {
        public static HashSet<string> s_scope
        {
            get
            {
                return new HashSet<string>(new[] { "r1/scope1", "r1/scope2" }, StringComparer.OrdinalIgnoreCase);
            }
        }

        public static readonly Dictionary<string, string> ExtraHttpHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "SomeExtraHeadderKey", "SomeExtraHeadderValue" } };

        public const string ScopeStr = "r1/scope1 r1/scope2";
        public const string ScopeStrFormat = "r{0}/scope1 r{0}/scope2";
        public const string MsiResource = "scope";
        public static readonly string[] s_graphScopes = new[] { "user.read" };
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes
        public const string ClientCredentialAudience = "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca/v2.0";
        public const string PublicCloudConfidentialClientID = "88f91eac-c606-4c67-a0e2-a5e8a186854f";
        public const string AutomationTestCertName = "LabAuth.MSIDLab.com";
        public static Dictionary<string, string> AdditionalAssertionClaims =>
            new Dictionary<string, string>() { { "Key1", "Val1" }, { "Key2", "Val2" }, { "customClaims", "{\"xms_az_claim\": [\"GUID\", \"GUID2\", \"GUID3\"]}" } };

        public static readonly SortedSet<string> s_scopeForAnotherResource = new SortedSet<string>(new[] { "r2/scope1", "r2/scope2" }, StringComparer.OrdinalIgnoreCase);
        public static readonly SortedSet<string> s_cacheMissScope = new SortedSet<string>(new[] { "r3/scope1", "r3/scope2" }, StringComparer.OrdinalIgnoreCase);
        public const string ScopeForAnotherResourceStr = "r2/scope1 r2/scope2";
        public const string Uid = "my-uid";
        public const string Utid = "my-utid";
        public const string Utid2 = "my-utid2";

        public const string Common = Constants.Common;
        public const string Organizations = Constants.Organizations;
        public const string Consumers = Constants.Consumers;
        public const string Guest = "guest";
        public const string Home = "home";
        public const string TenantId = "751a212b-4003-416e-b600-e1f48e40db9f";
        public const string TenantId2 = "aaaaaaab-aaaa-aaaa-bbbb-aaaaaaaaaaaa";
        public const string AadTenantId = "751a212b-4003-416e-b600-e1f48e40db9f";
        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        public const string MsftTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string SomeTenantId = "sometenantid";
        public const string CatsAreAwesome = "catsareawesome";
        public const string TenantIdNumber1 = "12345679";
        public const string TenantIdNumber2 = "987654321";
        public const string TenantIdString = "tenantid";
        public const string AadAuthorityWithTestTenantId = "https://login.microsoftonline.com/" + AadTenantId + "/";
        public const string AadAuthorityWithMsftTenantId = "https://login.microsoftonline.com/" + MsftTenantId + "/";
        public static readonly IDictionary<string, string> s_clientAssertionClaims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "client_ip", "some_ip" }, { "aud", "some_audience" } };
        public const string RTSecret = "someRT";
        public const string ATSecret = "some-access-token";
        public const string RTSecret2 = "someRT2";
        public const string ATSecret2 = "some-access-token2";
        public const string RTSecret3 = "someRT3";
        public const string ATSecret3 = "some-access-token3";

        public const string HomeAccountId = Uid + "." + Utid;

        public const string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";
        public const string ProductionPrefCacheEnvironment = "login.windows.net";
        public const string ProductionPrefRegionalEnvironment = "centralus.login.microsoft.com";
        public const string ProductionPrefInvalidRegionEnvironment = "invalidregion.login.microsoft.com";
        public const string ProductionNotPrefEnvironmentAlias = "sts.windows.net";
        public const string SovereignNetworkEnvironmentDE = "login.microsoftonline.de";
        public const string SovereignNetworkEnvironmentCN = "login.partner.microsoftonline.cn";
        public const string PpeEnvironment = "login.windows-ppe.net";
        public const string PpeOrgEnvironment = "login.windows-ppe.org"; //This environment is not known to MSAL or AAD

        public const string AuthorityNotKnownCommon = "https://sts.access.edu/" + Common + "/";
        public const string AuthorityNotKnownTenanted = "https://sts.access.edu/" + Utid + "/";

        public const string AuthorityHomeTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Home + "/";
        public const string AuthorityUtidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid + "/";
        public const string AuthorityUtid2Tenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid2 + "/";
        public const string AuthorityGuestTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Guest + "/";
        public const string AuthorityCommonTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Common + "/";
        public const string AuthorityRegional = "https://" + ProductionPrefRegionalEnvironment + "/" + TenantId + "/";
        public const string AuthorityRegionalInvalidRegion = "https://" + ProductionPrefInvalidRegionEnvironment + "/" + TenantId + "/";
        public const string AuthorityTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantId + "/";
        public const string AuthorityCommonTenantNotPrefAlias = "https://" + ProductionNotPrefEnvironmentAlias + "/" + Common + "/";
        public const string AuthorityCommonPpeAuthority = "https://" + PpeEnvironment + "/" + Common + "/";
        public const string AuthoritySovereignDETenant = "https://" + SovereignNetworkEnvironmentDE + "/" + TenantId + "/";
        public const string AuthoritySovereignCNTenant = "https://" + SovereignNetworkEnvironmentCN + "/" + TenantId + "/";
        public const string AuthoritySovereignDECommon = "https://" + SovereignNetworkEnvironmentDE + "/" + Common + "/";
        public const string AuthoritySovereignCNCommon = "https://" + SovereignNetworkEnvironmentCN + "/" + Common + "/";

        public const string PrefCacheAuthorityCommonTenant = "https://" + ProductionPrefCacheEnvironment + "/" + Common + "/";
        public const string AuthorityOrganizationsTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Organizations + "/";
        public const string AuthorityConsumersTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Consumers + "/";
        public const string AuthorityConsumerTidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + MsaTenantId + "/";
        public const string AuthorityGuidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantIdNumber1 + "/";
        public const string AuthorityGuidTenant2 = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantIdNumber2 + "/";
        public const string AuthorityWindowsNet = "https://" + ProductionPrefCacheEnvironment + "/" + Utid + "/";
        public const string ADFSAuthority = "https://fs.msidlab8.com/adfs/";
        public const string ADFSAuthority2 = "https://someAdfs.com/adfs/";

        public const string DstsAuthorityTenantless = "https://some.url.dsts.core.azure-test.net/dstsv2/";
        public const string DstsAuthorityTenanted = DstsAuthorityTenantless + TenantId + "/";
        public const string DstsAuthorityCommon = DstsAuthorityTenantless + Common + "/";
        public const string DstsAuthorityOrganizations = DstsAuthorityTenantless + Organizations + "/";
        public const string DstsAuthorityConsumers = DstsAuthorityTenantless + Consumers + "/";

        public const string GenericAuthority = "https://demo.duendesoftware.com";

        // not actually used by MSAL directly, MSAL will transform it to tenanted format
        public const string CiamAuthorityMainFormat = "https://tenant.ciamlogin.com/";
        public const string CiamAuthorityWithFriendlyName = "https://tenant.ciamlogin.com/tenant.onmicrosoft.com";
        public const string CiamAuthorityWithGuid = "https://tenant.ciamlogin.com/aaaaaaab-aaaa-aaaa-cccc-aaaaaaaaaaaa";

        public const string B2CLoginGlobal = ".b2clogin.com";
        public const string B2CLoginUSGov = ".b2clogin.us";
        public const string B2CLoginMoonCake = ".b2clogin.cn";
        public const string B2CLoginBlackforest = ".b2clogin.de";
        public const string B2CLoginCustomDomain = CatsAreAwesome + ".com";
        public const string B2CSignUpSignIn = "b2c_1_susi";
        public const string B2CProfileWithDot = "b2c.someprofile";
        public const string B2CEditProfile = "b2c_1_editprofile";
        public const string B2CEnvironment = SomeTenantId + ".b2clogin.com";
        public const string B2CAuthority = "https://login.microsoftonline.in/tfp/tenant/" + B2CSignUpSignIn + "/";
        public const string B2CLoginAuthority = "https://" + B2CEnvironment + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";
        public const string B2CLoginAuthorityWrongHost = "https://anothertenantid.b2clogin.com/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";
        public const string B2CCustomDomain = "https://" + B2CLoginCustomDomain + "/tfp/" + CatsAreAwesome + "/" + B2CSignUpSignIn + "/";
        public const string B2CLoginAuthorityUsGov = "https://" + SomeTenantId + B2CLoginUSGov + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";
        public const string B2CLoginAuthorityMoonCake = "https://" + SomeTenantId + B2CLoginMoonCake + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";
        public const string B2CLoginAuthorityBlackforest = "https://" + SomeTenantId + B2CLoginBlackforest + "/tfp/" + SomeTenantId + "/" + B2CSignUpSignIn + "/";
        public const string B2CSuSiHomeAccountIdentifer = Uid + "-" + B2CSignUpSignIn + "." + Utid;
        public const string B2CSuSiHomeAccountObjectId = Uid + "-" + B2CSignUpSignIn;
        public const string B2CProfileWithDotHomeAccountIdentifer = Uid + "-" + B2CProfileWithDot + "." + Utid;
        public const string B2CProfileWithDotHomeAccountObjectId = Uid + "-" + B2CProfileWithDot;
        public const string B2CEditProfileHomeAccountIdentifer = Uid + "-" + B2CEditProfile + "." + Utid;
        public const string B2CEditProfileHomeAccountObjectId = Uid + "-" + B2CEditProfile;

        public const string ClientId = "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3";
        public const string ClientId2 = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        public const string ObjectId = "593b2662-5af7-4a90-a9cb-5a9de615b82f";
        public const string FamilyId = "1";
        public const string UniqueId = "unique_id";
        public const string IdentityProvider = "my-idp";
        public const string Name = "First Last";
        public const string MiResourceId = "/subscriptions/ffa4aaa2-4444-4444-5555-e3ccedd3d046/resourcegroups/UAMI_group/providers/Microsoft.ManagedIdentityClient/userAssignedIdentities/UAMI";
        public const string VmId = "test-vm-id";
        public const string VmssId = "test-vmss-id";

        public const string Claims = @"{""userinfo"":{""given_name"":{""essential"":true},""nickname"":null,""email"":{""essential"":true},""email_verified"":{""essential"":true},""picture"":null,""http://example.info/claims/groups"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";
        public static readonly string[] ClientCapabilities = new[] { "cp1", "cp2" };
        public const string ClientCapabilitiesJson = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]}}}";
        // this a JSON merge from Claims and ClientCapabilitiesJson
        public const string ClientCapabilitiesAndClaimsJson = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]}},""userinfo"":{""given_name"":{""essential"":true},""nickname"":null,""email"":{""essential"":true},""email_verified"":{""essential"":true},""picture"":null,""http://example.info/claims/groups"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";
        public const string ClaimsWithAccessToken = @"{""access_token"":{""nbf"":{""essential"":true, ""value"":""1701477303""}}}";
        public const string ClientCapabilitiesAndClaimsJsonWithAccessToken = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""}}}";
        public const string EmptyClaimsJson = @"{}";
        public const string ClaimsWithAdditionalClaim = @"{""access_token"":{""nbf"":{""essential"":true, ""value"":""1701477303""},""additional_claim"":{""key"":""value""}}}";
        public const string MergedJsonWithAdditionalClaim = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""}}}";
        public const string ClaimWithAdditionalKey = @"{""access_token"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";
        public const string MergedJsonWithAdditionalKey = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";
        public const string ClaimWithAdditionalKeyAndAccessKey = @"{""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""access_token"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";
        public const string MergedJsonClaimWithAdditionalKeyAndAccessKey = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]},""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}},""some_other_key"":{""nbf"":{""essential"":true,""value"":""1701477303""},""additional_claim"":{""key"":""value""},""new_claim"":{""new_key"":""new_value""}}}";
        public const string ClaimsChallenge = "eyJhY2Nlc3NfdG9rZW4iOnsiYWNycyI6eyJlc3NlbnRpYWwiOnRydWUsInZhbHVlIjoiY3AxIn19fQ==";

        public const string DisplayableId = "displayable@id.com";
        public const string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public const string MobileDefaultRedirectUri = "msal4a1aa1d5-c567-49d0-ad0b-cd957a47f842://auth"; // in msidentity-samples-testing tenant -> PublicClientSample
        public const string ClientSecret = "client_secret";
        public const string DefaultPassword = "password";
        public const string TestCertPassword = "passw0rd!";
        public const string AuthorityTestTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid + "/";
        public const string DiscoveryEndPoint = "discovery/instance";
        public const string DefaultAuthorizationCode = "DefaultAuthorizationCode";
        public const string DefaultAccessToken = "DefaultAccessToken";
        public const string DefaultClientAssertion = "DefaultClientAssertion";
        public const string RawClientId = "eyJ1aWQiOiJteS11aWQiLCJ1dGlkIjoibXktdXRpZCJ9";
        public const string XClientSku = "x-client-SKU";
        public const string XClientVer = "x-client-Ver";
        public const TokenSubjectType TokenSubjectTypeUser = 0;
        public const string TestMessage = "test message";
        public const string LoginHint = "loginHint";
        public const string LoginHintParam = "login_hint";
        public const string PromptParam = "prompt";

        public const string LocalAccountId = "test_local_account_id";
        public const string GivenName = "Joe";
        public const string FamilyName = "Doe";
        public const string Username = "joe@localhost.com";
        public const string Email = "joe@contoso.com";
        public const string PKeyAuthResponse = "PKeyAuth Context=\"context\",Version=\"1.0\"";

        public const string RegionName = "REGION_NAME";
        public const string Region = "centralus";
        public const string InvalidRegion = "invalidregion";
        public const int TimeoutInMs = 2000;
        public const string ImdsHost = "169.254.169.254";
        public const string ImdsUrl = $"http://{ImdsHost}/metadata/instance/compute/location";

        public const string AppServiceEndpoint = "http://127.0.0.1:41564/msi/token";
        public const string AzureArcEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        public const string CloudShellEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        public const string ImdsEndpoint = $"http://{ImdsHost}/metadata/identity/oauth2/token";
        public const string MachineLearningEndpoint = "http://localhost:7071/msi/token";
        public const string ServiceFabricEndpoint = "https://localhost:2377/metadata/identity/oauth2/token";

        public const string UserAssertion = "fake_access_token";
        public const string CodeVerifier = "someCodeVerifier";

        public const string Nonce = "someNonce";
        public const string Realm = "someRealm";

        public const string TestErrCode = "TestErrCode";
        public const string iOSBrokerSuberrCode = "TestSuberrCode";
        public const string iOSBrokerErrDescr = "Test Error Description";
        public const string iOSBrokerErrorMetadata = "error_metadata";
        public const string iOSBrokerErrorMetadataValue = @"{""home_account_id"":""test_home"", ""username"" : """ + Username + @""" }";
        public const string DefaultGraphScope = "https://graph.microsoft.com/.default";

        public const string Bearer = "Bearer";
        public const string Pop = "PoP";
        public const string FmiNodeClientId = "urn:microsoft:identity:fmi";

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

        public const string MsalCCAKeyVaultUri = "https://buildautomation.vault.azure.net/secrets/AzureADIdentityDivisionTestAgentSecret/";
        public const string MsalCCAKeyVaultSecretName = "MSIDLAB4-IDLABS-APP-AzureADMyOrg-CC";
        public const string MsalOBOKeyVaultUri = "https://buildautomation.vault.azure.net/secrets/IdentityDivisionDotNetOBOServiceSecret/";
        public const string MsalOBOKeyVaultSecretName = "IdentityDivisionDotNetOBOServiceSecret";
        public const string MsalArlingtonOBOKeyVaultUri = "https://msidlabs.vault.azure.net:443/secrets/ARLMSIDLAB1-IDLASBS-App-CC-Secret";
        public const string MsalArlingtonOBOKeyVaultSecretName = "ARLMSIDLAB1-IDLASBS-App-CC-Secret";
        public const string MsalArlingtonCCAKeyVaultUri = "https://msidlabs.vault.azure.net:443/secrets/ARLMSIDLAB1-IDLASBS-App-CC-Secret";
        public const string MsalArlingtonCCAKeyVaultSecretName = "ARLMSIDLAB1-IDLASBS-App-CC-Secret";

        public enum AuthorityType { B2C };
        public static string[] s_prodEnvAliases = new[] {
                                "login.microsoftonline.com",
                                "login.windows.net",
                                "login.microsoft.com",
                                "sts.windows.net"};

        public static readonly string s_userIdentifier = CreateUserIdentifier();

        public static string CreateUserIdentifier()
        {
            // return CreateUserIdentifier(Uid, Utid);
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Uid, Utid);
        }

        public static string CreateUserIdentifier(string uid, string utid)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", uid, utid);
        }

        public static MsalTokenResponse CreateMsalTokenResponse(string tenantId = null)
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

        public static MsalTokenResponse CreateMsalTokenResponseWithTokenSource()
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

        public static readonly Account s_user = new Account(s_userIdentifier, DisplayableId, ProductionPrefNetworkEnvironment);

        public const string OnPremiseAuthority = "https://fs.contoso.com/adfs/";
        public const string OnPremiseClientId = "on_premise_client_id";
        public const string OnPremiseUniqueId = "on_premise_unique_id";
        public const string OnPremiseDisplayableId = "displayable@contoso.com";
        public const string FabrikamDisplayableId = "displayable@fabrikam.com";
        public const string OnPremiseHomeObjectId = OnPremiseUniqueId;
        public const string OnPremisePolicy = "on_premise_policy";
        public const string OnPremiseRedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";
        public const string OnPremiseClientSecret = "on_premise_client_secret";
        public const string OnPremiseUid = "my-OnPremise-UID";
        public const string OnPremiseUtid = "my-OnPremise-UTID";

        public static readonly Account s_onPremiseUser = new Account(
            string.Format(CultureInfo.InvariantCulture, "{0}.{1}", OnPremiseUid, OnPremiseUtid), OnPremiseDisplayableId, null);

        public const string BrokerExtraQueryParameters = "extra=qp&key1=value1%20with%20encoded%20space&key2=value2";
        public const string BrokerOIDCScopes = "openid offline_access profile";
        public const string BrokerClaims = "testClaims";

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

        public const string DiscoveryFailedResponse =
            @"{""error"":""invalid_instance"",
               ""error_description"":""AADSTS50049: Unknown or invalid instance.\r\nTrace ID: 82e709b9-f0b3-431d-99cd-f3c2ca3d4b00\r\nCorrelation ID: e7619cf4-53ea-443c-b76a-194c032e9840\r\nTimestamp: 2021-04-14 11:27:26Z"",
               ""error_codes"":[50049],
               ""timestamp"":""2021-04-14 11:27:26Z"",
               ""trace_id"":""82e709b9-f0b3-431d-99cd-f3c2ca3d4b00"",
               ""correlation_id"":""e7619cf4-53ea-443c-b76a-194c032e9840"",
               ""error_uri"":""https://login.microsoftonline.com/error?code=50049""}";

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

        // constants for Azure AD Kerberos Features
        public const string KerberosTestApplicationId = "682992e9-c9c6-49c9-a819-3fbca2dd5111";
        public const string KerberosServicePrincipalName = "HTTP/msal-kerberos-test.msidlab4.com";
        public const string KerberosServicePrincipalNameEscaped = "HTTP**msal-kerberos-test.msidlab4.com";
        public const string AzureADKerberosRealmName = "KERBEROS.MICROSOFTONLINE.COM";
        public const int KerberosMinMessageBufferLength = 256;

        // do not change these constants!
        public const string AadRawClientInfo = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0";
        public const string MsaRawClientInfo = "eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ";
        public const string B2CRawClientInfo = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9";

        //Region Discovery Failures
        public const string RegionAutoDetectOkFailureMessage = "Call to local IMDS failed with status code OK or an empty response.";
        public const string RegionAutoDetectNotFoundFailureMessage = "Call to local IMDS failed with status code NotFound or an empty response.";
        public const string RegionAutoDetectInternalServerErrorFailureMessage = "Service is unavailable to process the request";
        public const string RegionDiscoveryNotSupportedErrorMessage = "Region discovery can only be made if the service resides in Azure function or Azure VM";
        public const string RegionDiscoveryIMDSCallFailedMessage = "IMDS call failed";

        public const string PiiSerializeLogMessage = "MsalExternalLogMessage: Serializing Cache Pii";
        public const string PiiDeserializeLogMessage = "MsalExternalLogMessage: Deserializing Cache Pii";
        public const string SerializeLogMessage = "MsalExternalLogMessage: Serializing Cache without Pii";
        public const string DeserializeLogMessage = "MsalExternalLogMessage: Deserializing Cache without Pii";

        public const string GenericOidcJwkResponse = @"{""keys"":[{""kty"":""RSA"",""use"":""sig"",""kid"":""66682C848A3140685FC883FD7EA993CC"",""e"":""AQAB"",""n"":""pY-a5km28zOE-KS1UgYlWS9AT-4eYdxAlTVeGaSq21dhbB4L6tmlUiiV8s-Zv_L5Ng6rC1asmjEVtrKmFkYMoW4RbJC6HAzQbS7crGglyTJ39uDGJBpeQZCWYUljlIzp2VAJnPxG1-iyIDjZSOuGgvTxiphV4j2naU46RcT3IfC7CPkUZUtmqpbYNOHRli_oVirxGUMjHbq623qOCQUkUfMBLhKr0EjrZtcispSDzHqWktUO7K8Iy8D6VyttPIuzVkYx1GYiB0jCF1jgIDyEnH1E3r6S5ytao9KvoO6DGZTzFTJL2-i_uPco1DXfXFlVO9jKb5MHomO3NNrSDNRSnQ"",""alg"":""RS256""}]}";
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

        public static MsalTokenResponse CreateAadTestTokenResponse()
        {
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.\",\"client_info\":\"" + AadRawClientInfo + "\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        public static MsalTokenResponse CreateMsaTestTokenResponse()
        {
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Tasks.Read User.Read openid profile\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJ2ZXIiOiIyLjAiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vOTE4ODA0MGQtNmM2Ny00YzViLWIxMTItMzZhMzA0YjY2ZGFkL3YyLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwiYXVkIjoiYjZjNjlhMzctZGY5Ni00ZGIwLTkwODgtMmFiOTZlMWQ4MjE1IiwiZXhwIjoxNTM4ODg1MjU0LCJpYXQiOjE1Mzg3OTg1NTQsIm5iZiI6MTUzODc5ODU1NCwibmFtZSI6IlRlc3QgVXNlcm5hbWUiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtc2Fsc2RrdGVzdEBvdXRsb29rLmNvbSIsIm9pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInRpZCI6IjkxODgwNDBkLTZjNjctNGM1Yi1iMTEyLTM2YTMwNGI2NmRhZCIsImFpbyI6IkRXZ0tubCFFc2ZWa1NVOGpGVmJ4TTZQaFphUjJFeVhzTUJ5bVJHU1h2UkV1NGkqRm1CVTFSQmw1aEh2TnZvR1NHbHFkQkpGeG5kQXNBNipaM3FaQnIwYzl2YUlSd1VwZUlDVipTWFpqdzghQiIsImFsZyI6IkhTMjU2In0.\",\"client_info\":\"" + MsaRawClientInfo + "\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        public static MsalTokenResponse CreateB2CTestTokenResponse()
        {
            const string jsonResponse = "{\"access_token\":\"<removed_at>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIn0.\",\"token_type\":\"Bearer\",\"not_before\":1538801260,\"expires_in\":3600,\"ext_expires_in\":262800,\"expires_on\":1538804860,\"resource\":\"14df2240-96cc-4f42-a133-ef0807492869\",\"client_info\":\"" + B2CRawClientInfo + "\",\"scope\":\"https://iosmsalb2c.onmicrosoft.com/webapitest/user.read\",\"refresh_token\":\"<removed_rt>\",\"refresh_token_expires_in\":1209600}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        public static MsalTokenResponse CreateB2CTestTokenResponseWithTenantId()
        {
            const string jsonResponse = "{\"access_token\":\"<removed_at>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIiwidGlkIjoiYmE2YzBkOTQtYThkYS00NWIyLTgzYWUtMzM4NzFmOWMyZGQ4IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20ifQ.\",\"token_type\":\"Bearer\",\"not_before\":1538801260,\"expires_in\":3600,\"ext_expires_in\":262800,\"expires_on\":1538804860,\"resource\":\"14df2240-96cc-4f42-a133-ef0807492869\",\"client_info\":\"" + B2CRawClientInfo + "\",\"scope\":\"https://iosmsalb2c.onmicrosoft.com/webapitest/user.read\",\"refresh_token\":\"<removed_rt>\",\"refresh_token_expires_in\":1209600}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        public static MsalTokenResponse CreateAadTestTokenResponseWithFoci()
        {
            const string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.\",\"client_info\":\"" + AadRawClientInfo + "\",\"foci\":\"1\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        // Fake strings approximately representing tokens of real-world size
        internal const string AppAccessToken = "a9sVdA2UrU0KbsG19MjSZp4hlrd4B3mhZc1gfWsu1v3Nxuf9y7MMqdmCqyt4OwJ7cZEFAhdy8wf9vRastaS0Dcc6MnAtaI73k9co06QtQgecLPVMR23ht6cWB25Ta7yqdcQ8X7ARdUD4MWY6o01TqADKE4Vo3DMpzZiwpiS6Z81I5bW9jbWiUTT7J44lRM1qR3ZJUdaa6OSOaLZT4temLYy3bXZh6Bvu7fxOF1pStzwESRDiFegYq6LFf1sHVY5scdvNOifQnWuRy4bWw0Fl17IO5lKzhuEUvaOcgyWea0Hg3Rh9U0or2WPogJna7HraHdp0BWtuj3KOdXqmjxDoFKkTfBXiubpnAqlWwfAKHvKYefNuSRT7ewHLFQNpVVbr93P1Na4aMHqL4DWIQ1BEYoRskQkxNDtzeipb5CP5FrfStz8lFB4nCaZsJPqIzi82BpZ4HwXls8VYrRLt2dK0D9ksxufhN2uUknO2w5MBDvpwl5IuPZiCvwxU0IG3eDyqRCqw1uh18KL4qbeAwp0BPQL7Xexc4eCXC5o24uxipyWK6C169R645sYCgHY9Phiik9fUaTcZ6rzPhSKXK5svhtuMUpDHoHnSkOocCmJPRdNEpULup7zAMtGxGtD4ldfRRtAD5MLBcnKB4QN4KsbysV9lj1xDSFawL22pno61hreo5lJmqlRdj3yRVupqg5IRhilWI0wbUSfUPISrBS0EcCb6rSBd6MPIbstJYOsuJ4Bh4wtLDDeD5oxlMIaF2tkf2QHfspXocd0fLbpA7X9AR1s5yYRRJKgh7SJMgS4JJxLHvF9VZJCKKwnYzlR5EaHYsvDCGyvEi9UkWDCBYMcG6OvzYemjVOTiJTbNM2q6DaqLS9bOMZdZzNfG3PlMAqeZlH5CLv8edLzfDaVZmMCdsc6iUTCAgW4ERDzu7Pf2AeQHQlpplPkgYMxiKZ31BxoaHvwlGReopWq3NN8oOS9QG5VCG1osQRmBZAQHDZTA2x30C4l8L4yH8tvs12trqAvRGnFIq79qluVwJHAPGbQZiAEArzpN7laNqDmzJFCA4emv6DdnCnGdLCSaIubVTBJ5dXbBN9xmphJUGw1jCoFDcFxrcOK0MdJnVGnbkEuSrnEVHUGmocUG1tb0QRZ9Jhn9mGO6RxVcZVzs7XJi8vEQ0FeWJOG0uqsNyi0gCmsglyH8QqsOVlYve8UuxqCgzm1CgfCeNDJEB01xk6iTsjRM0iGcCaZGCm9dRdW99Hlq8SsLRZl2IT48KYj4B9ZP4jBrkY9Ef3xpFRu8fiwgBdzrZosZA2aQPRXlvdM1XjeGaK7iUsrpsVOrvDorBoMucV3uypI85c7yQaTu1qrKfri7OZgEVVBdQO08iipyvyRVSBz1U9ZAc0xqgERqPUoPIOFtqMRx1qJD7WWRUa0hnd66SAgiM2ViGBZspIsUvA9KnAF28uU0kVRn3FXb3B3pNLhpHQSqElPz1uFKp6gO1kqRR6B6TwKn";
        internal const string UserAccessToken = "flMpQIKiCoiPK6qISSjmF9dGhKe47KFGPwe82BDBxBCVfYI4UiKYbBuShsjf8oGTsjN5ODeaO6k0cmZJYuNNbLyOr8JGqoxQRW9bI8j5ETpbTNf6tYpAWde9PIYj2wEBnbughVgtJsh2QxIrahie5leMpsGb1yoFzADD5gyoJq8etNUSgZwe5qkfaE9UBCUKrznKjKbsG5hBJXut5GD0QdQy3wo2PnocewrptlMzd5SsHCzUUBGA4q7ks7IfrLiQH11JyBnjBhypOX3XvuqBz4JKkpftVYfvwPWE3f5Onku6FkZJFFESyGQP9YnJVx5dQCpHH9l6ShTqOLSQduf7wxoyeAgxwPrM9Y8Kvj31IrXqiwP52x4hBsctLCqOXOZ3wMXnozMXyHpNvKMJaNgDgvBgMYhiyORkb3qKYw0gAP4659I8dK1esxJoD8I3EreDftGfNMFCgn7kFfauUQphkqx8ukqzw068R7g5TOUci1pgPcVXCAMxj0P3fTiKe1doVuF6znKYh3m7pjyzyaqb5K9VFIh4A8TXOO0MqjaVkoSWJXARTy4T0kAZBVPbO6U2BWku23yLIt43MhQTc9uf7inuirwaIgh5u7noDxYG4QZLB1CJl04Zq2gbh9GW7dqweAaC9efYTEDwhxDTPHeGTQs44e8cnWerIyZA7mq8sFuzihIiCfgZ6nNBPcx2lXKyarUtQGmjjRyOEAhs66atv3SgMhNBhontPoUhR1QEnTKeYzfaavlnf5qMZA41hijGazHyxy5FgLD5aLEpZTHN5MPQLeaEXzDMX5Wtdvq7nokiItRfLkKZtXkuSiFVltmRPcKqzGbjNRH96OQzuxLE1Mv25FYFR3PAwv6np69yScVOpNFL8CqJdT310dGnRPUKSrEqTPuMsHqVRr36j2ZUaGs6YBtcrxIxKHuPrv23FQg5fC0FgxZvKqve0hf68AocJ1HqKRy01CGQobmYpTwBByftOZYGC4KOfGd13l78kZaKLuk2gxfFuTQyr11A0L4n5tXfjlikJtr3wlTGt0KCGGXmNK1xsSoRC0VcXDOgQUu3FHblhiaYjbSvPRF09xn9tRPnUkznbsT1kPMiJ8v89ZOCtVWpvkoiy9VUVcSUpZNQwRh3wHidZAkp1xyjyVc2pIHPg6XhzJnlt77zHNiBkPxWbYt7hXBQf3QeYoMF4s0Qi1y5N72DdoSNJ3iaTwx3esAz6TeyxSh36PIz35mR5jGyGMssyaNg6lIewLPbjnizgC6xssi6mKOheDqWqBv89nIvSBOXEkKcUYsBlhBBK6BgxOIha1NAeP93RRKfyjrF7LtIoSOk3DJUx75rUJ9oyuuTt4FdSnp7ZdrIciO8vlNslPrfa7UjBdOtVHiaz9Ef91dctdADVFcwXXmcu2ypyKB1YvMbkPP7mc12TF1a8X6t0mU4s4J4IpA3SHmT5JvbQBEzOIs6ex38X3UtXSItxpaS2gKozAhAmvjt6NKMe3Jysm4bafH1kb8eB1vdwTQu3jIOGozqHC3rvqEVAt26NNKOuNYAoYYamQOSb2w8PUCuDDWs1ffLvvfyvRndZztV5C4HGGR1Tg82N291Sb7rSUYmA1rdGyJ4kPtSaiPOwMyPUs9FuZNef5Ib83D3gTcgS1gMxto5UkfSxtCDKLXtGKArOdACrRzHiiMSn3owQfyVtSXZPdeofoCzuPWcZzFLBUJR0iKWBpUkxd0N17vw45uMQpQUNGgGoyvyboKkAFlOGsEIAmrnooC3CJGVA4jHPYJnVG4xTJ37U6QL5sX95qWtjbvuD5KoT2GyWec0o62CNr09tCQsiALLC1QrfCiCGsullefbsgBB5tsOY1Kyiy4uf84qBMu20GbsJ01R8xxpJ5bh6HFRaStEK3WIy7TMJym42YMbxB3AGsGFGhNYljtuqgeUjXn1UuWskkB6QqdepFHCof6CHg0LlV0o4Iz9QKu5cfoi8jk5HKbvIGyDqCgZaC2LdugNgQ0X";
        internal const string RefreshToken = "mhJDJ8wtjA3KxpRtuPAreZnMcJ2yKC2JUbpOGbRTdOCImLyQ2B4EIhv8AiA2cCEylZZfZsOsZrNsMBZZAAU9TQYYEO72QcdfnIWpAOeKkud5W2L8nMq6i9dx1EVIl09zFXhOJ79BdFbU0Eb5aUHlcqPCQjec62UKBLkZJmtMnoAa8cjvgIuxTdVM8FNdghe5nlCNTEVooKleTTEHNl2BrdyitLaWTKSP0lRqnFxriG0xWcJoSMsdS7Vt6HZd1TkwHIXycNMlCcCdUh5tOgqx1M8y8uoXK4OJ1LQmtkZvcQWcycvOCPACYakKM1pUQqwTxI6Y4HrL38sqQaSNxpF9OcFxOQWpuGodRekCbxXVbWclttIpvSOLaBhZ2ZBpcCBEeEMSmhqqYgajNwwwe9w88u0UsYKe6PBbaI48ENr02u2qBeLsIQ2HUyKlN3iVmX7u7MhgDWA3NNavMtlLmWd63NfuDgXpLI0O4cLhjAx8uoBIK8LntXPHPTxJ28o0yrszvD4gf7RdhuTq5VE15zne6iAJgIGfy7latGFzxuDMcML9OoXURHnNEHBgS9ZQCfNzYZ2O9flF1UjGpcBLEi7hHVHnrQb4y7c98dz9p62cvEMhorGx9kCwSIkOae5LheXPQkFIbsGyomNEwz3HZvR131VGAwdfmUUodvPr6LAAtmjl4sZ72PRqAo8EdQ0IFsWoypXVv51IooR87tO3uiG2DkxhIAwumOQdaJNxw1a0WS9mpQOmwFlvfbZkaIoUKgagHc8fVa1aHZntLGwH0S1iYixJiIrMnPYAeRdSp9mlHllrMX8xUIznobcZ5i8MpUYCKlUXMZ82S3XUJ5dJxARNRPxXlLJ5LPYBhUNkBLQen9Qmq3VZEV1RDJyhbGp6GAo14KsMtVAVYNmYPIgo85pCZgOwVEOBUycszu4AD3p4PT2ella4LVoqmTTMSA5GEWoeWb5JvEo222Z0oKr7UK8dGwpWRSbg8TNeODihJaTUDfErvbgaZnjIRpqfgtM5i1HfQbD7Yyft5PqyygUra7GYy7pjRrEvq95XQD8sAZ32ku9AqCo5qOB584iX881WErOoheQZokt1txqwuIMUyhVuMKNEXy70CeNTsb30ghQMZpZcXIkrLYyQCZ0gNmARhMKagCSdrpUtxudLk44yfmuwSQzBN3ifWfLZiFpU53qdPLZoTw5";
        internal const string IdToken = "6GwdM7f6hHXfivavPozhaRqrbxvEysfXSMQyEKBwVgivPZTtmowsmYygchhIuxjeFFeq1ZPHjhxKFnulrvoY6TDerZY5xyOlg45bToI9Bu95qFvUrrt5r17UJcXdw4YkvEt10CcDDcLcEYw704RpVefvbpjbF24pOgIuafcAkDnbDA0Qea4ePuSC45Lw7zpJhbo9Gh8IfMX597fayBvMs3fh7frrm9KpWMCeKY3h99YSaCYjZFKp1ppvXXPE9bc4sh4pRDOfnv0Yr9J8u4elZevEE4qGddfgd3hYb18XPGRjPEMlWsh7tnwxwUm6OSZlMTHYuvwBENNMx7SUQmMeg4rCfgnbcNDkWpXCiSDVt1lLLv8F2GjYnM6De3v1Ks5lhBWx3grLggcN9LnXz92eJ1l5lTB2v0y9MgmFZ4gY43oIOW5n8G5HOx3bGOyjTw0TKKbyVa3mDj0A3QqW8eLTUJz42BNiGOf5m9prMSlpAW59CHCMJLatsj3IvGeCITsGAr3sUZEytORWUdxCfuIPwecQgU6bO7pNqNvZc1tJHHNwJlfS23ZkiFuEXqEThHYfxBCFxAzMDlzO0TOdWhvrb8hlNeAOcNhoAKxu7HXsePajKs4fU1rcdSxzNKwtASEla3p6jfJnnDtKf38RJZPaRRYMviqqWEMhjmqIvBm7sMaf8RyNNuYl7otZwmwNVCR1hzzmaTAy4kQce67FJqFba7uizrgwp9zsvK8muCHKKPvNthy7fHsxKmrBIm0bLcoePKK3wAID4kFvNQcxXp6rAOr8bLFF3bLEoYdzmF2QJz1frVZZHHPy90Cmlhw48EQN8NE2OllpdaykKt5k4rPcZQyitayNNhism30qh7eCBhcA7mm5Ja0S8X4VPlkwvgwg0mQuul6gakmja8xpnTrwiOdtao320GDmJaJA6zf3UTpNZTq9tdfBtUrjAD8RS0tNUBT3Ko8N2Lfh9ry8y9ESmRVIhch3rKY7UeefFAnkiwH2WwC57ZEsHtMP0SwKYtYKHZW9HkERCCyqOT1Mw0IavsLGFvchzMAvTnz4RwRBk6IrWgANvqT3F3Vexc2K0poKb71XZ4aMXxjqAzydGQAKpKJEJcqEvX9RD8nL76TF2LZIepiaZ3dbQImkqSjbF7aaY2JFoN9ZWlcSQKe8zdO8TIG16bF8W9R4ldDyzV39L33KcweG";

        #region Test Certificate and Private Key (ValidPemCertificate & XmlPrivateKey)
        /// <summary>
        /// A test PEM-encoded X.509 certificate and its matching RSA private key.
        /// These are used together in unit tests that require both a certificate and its private key.
        /// The <see cref="ValidPemCertificate"/> and <see cref="XmlPrivateKey"/> are a matched pair:
        /// - <see cref="ValidPemCertificate"/> is a PEM-encoded certificate.
        /// - <see cref="XmlPrivateKey"/> is the corresponding RSA private key in XML format.
        /// The certificate is valid for 100 years, ensuring it will not expire during the lifetime of the tests.
        /// </summary>
        internal const string ValidPemCertificate = @"-----BEGIN CERTIFICATE-----
MIIDATCCAemgAwIBAgIUSfjghyQB4FIS41rWfNcZHTLE/R4wDQYJKoZIhvcNAQEL
BQAwDzENMAsGA1UEAwwEVGVzdDAgFw0yNTA4MjgyMDIxMDBaGA8yMTI1MDgwNDIw
MjEwMFowDzENMAsGA1UEAwwEVGVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCC
AQoCggEBALlc0S6TdwgQKGRl3Y/9uWNRpWo1WHiZtd1YdgCBt0rjxTqsbQUurU0B
9Kdk7QQ9srxmjimxGHaUFypbb39awqIdQQcuQvIUj5+sQh9zzCyR35bGQp8vwbna
5GlhAIbzsUi/y5kEGUMbuQN05XfoJSQrU35XZ8duQSDH5h9aDr6kuLcpDHo9/9vZ
iosPfqGPxZGtVjMvrJdVQGLJF35xD3LlX8xG2iJfVK/xYQVi3MgbRNQaL2lHtZaG
Ac1CToMUPO60xXrZkQE08hC907YTBcavUVQg4vrOaPpsCs+Fj6EJcasADAJeh1mG
Bn3kHFPCxBa2MKFraFPp53zOagTvYV0CAwEAAaNTMFEwHQYDVR0OBBYEFA9irQR/
O6/V2JVyDEHFOdUDjAsyMB8GA1UdIwQYMBaAFA9irQR/O6/V2JVyDEHFOdUDjAsy
MA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEBAAOxtgYjtkUDVvWz
q/lkjLTdcLjPvmH0hF34A3uvX4zcjmqF845lfvszTuhc1mx5J6YLEzKfr4TrO3D3
g2BnDLvhupok0wEmJ9yVwbt1laim7zP09gZqnUqYM9hYKDhwgLZAaG3zGNocxDEA
U7jazMGOGF7TweB7LdNuVI6CqgDOBQ8Cy2ObuZvzCI5Y7f+HucXpiJOu1xNa2ZZp
MpQycYEvi5TD+CL5CBv2fcKQRn/+u5B3ZXCD2C9jT/RZ7rH46mIG7nC7dS4J2o4J
jmlJIUAe2U6tRay5GvEmc/nZK8hd9y4BICzrykp9ENAoy9i+uaE1GGWeNgO+irrc
rAcLwto=
-----END CERTIFICATE-----";
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

    internal static class Adfs2019LabConstants
    {
        public const string Authority = "https://fs.msidlab8.com/adfs";
        public const string AppId = "TestAppIdentifier";
        public const string PublicClientId = "PublicClientId";
        public const string ConfidentialClientId = "ConfidentialClientId";
        public const string ClientRedirectUri = "http://localhost:8080";
        public static readonly SortedSet<string> s_supportedScopes = new SortedSet<string>(new[] { "openid", "email", "profile" }, StringComparer.OrdinalIgnoreCase);
        public const string ADFS2019ClientSecretURL = "https://buildautomation.vault.azure.net/secrets/ADFS2019ClientCredSecret/";
        public const string ADFS2019ClientSecretName = "ADFS2019ClientCredSecret";
    }
}
