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
        public static readonly string[] s_graphScopes = new[] { "user.read" };
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes
        public const string ClientCredentialAudience = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0";
        public const string AutomationTestThumbprint = "378938210C976692D7F523B8C4FFBB645D17CE92";

        public static readonly SortedSet<string> s_scopeForAnotherResource = new SortedSet<string>(new[] { "r2/scope1", "r2/scope2" }, StringComparer.OrdinalIgnoreCase);
        public static readonly SortedSet<string> s_cacheMissScope = new SortedSet<string>(new[] { "r3/scope1", "r3/scope2" }, StringComparer.OrdinalIgnoreCase);
        public const string ScopeForAnotherResourceStr = "r2/scope1 r2/scope2";
        public const string Uid = "my-uid";
        public const string Utid = "my-utid";
        public const string Utid2 = "my-utid2"; 

        public const string Common = "common";
        public const string TenantId = "751a212b-4003-416e-b600-e1f48e40db9f";
        public const string TenantId2 = "aaaaaaab-aaaa-aaaa-bbbb-aaaaaaaaaaaa";
        public const string AadTenantId = "751a212b-4003-416e-b600-e1f48e40db9f";
        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        public const string AadAuthorityWithTestTenantId = "https://login.microsoftonline.com/751a212b-4003-416e-b600-e1f48e40db9f/";
        public static readonly IDictionary<string, string> s_clientAssertionClaims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "client_ip", "some_ip" }, { "aud", "some_audience" }};
        public const string RTSecret = "someRT";
        public const string ATSecret = "some-access-token";
        public const string RTSecret2 = "someRT2";
        public const string ATSecret2 = "some-access-token2";

        public const string HomeAccountId = Uid + "." + Utid;

        public const string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";
        public const string ProductionPrefCacheEnvironment = "login.windows.net";
        public const string ProductionPrefRegionalEnvironment = "centralus.r.login.microsoftonline.com";
        public const string ProductionPrefInvalidRegionEnvironment = "invalidregion.r.login.microsoftonline.com";
        public const string ProductionNotPrefEnvironmentAlias = "sts.windows.net";
        public const string PpeEnvironment = "login.windows-ppe.net";

        public const string AuthorityNotKnownCommon = "https://sts.access.edu/common/";
        public const string AuthorityNotKnownTenanted = "https://sts.access.edu/" + Utid + "/";

        public const string SovereignNetworkEnvironment = "login.microsoftonline.de";
        public const string AuthorityHomeTenant = "https://" + ProductionPrefNetworkEnvironment + "/home/";
        public const string AuthorityUtidTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid + "/";
        public const string AuthorityUtid2Tenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid2 + "/";
        public const string AuthorityGuestTenant = "https://" + ProductionPrefNetworkEnvironment + "/guest/";
        public const string AuthorityCommonTenant = "https://" + ProductionPrefNetworkEnvironment + "/common/";
        public const string AuthorityRegional = "https://" + ProductionPrefRegionalEnvironment + "/" + TenantId + "/";
        public const string AuthorityRegionalInvalidRegion = "https://" + ProductionPrefInvalidRegionEnvironment + "/" + TenantId + "/";
        public const string AuthorityTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + TenantId + "/";
        public const string AuthorityCommonTenantNotPrefAlias = "https://" + ProductionNotPrefEnvironmentAlias + "/common/";
        public const string AuthorityCommonPpeAuthority = "https://" + PpeEnvironment + "/common/";

        public const string PrefCacheAuthorityCommonTenant = "https://" + ProductionPrefCacheEnvironment + "/common/";
        public const string AuthorityOrganizationsTenant = "https://" + ProductionPrefNetworkEnvironment + "/organizations/";
        public const string AuthorityConsumersTenant = "https://" + ProductionPrefNetworkEnvironment + "/consumers/";
        public const string AuthorityConsumerTidTenant = "https://" + ProductionPrefNetworkEnvironment + "/9188040d-6c67-4c5b-b112-36a304b66dad/";
        public const string AuthorityGuidTenant = "https://" + ProductionPrefNetworkEnvironment + "/12345679/";
        public const string AuthorityGuidTenant2 = "https://" + ProductionPrefNetworkEnvironment + "/987654321/";
        public const string AuthorityWindowsNet = "https://" + ProductionPrefCacheEnvironment + "/" + Utid + "/";
        public const string ADFSAuthority = "https://fs.msidlab8.com/adfs/";
        public const string ADFSAuthority2 = "https://someAdfs.com/adfs/";

        public const string B2CSignUpSignIn = "b2c_1_susi";
        public const string B2CProfileWithDot = "b2c.someprofile";
        public const string B2CEditProfile = "b2c_1_editprofile";
        public const string B2CEnvironment = "sometenantid.b2clogin.com";
        public static readonly string B2CAuthority = $"https://login.microsoftonline.in/tfp/tenant/{B2CSignUpSignIn}/";
        public static readonly string B2CLoginAuthority = $"https://sometenantid.b2clogin.com/tfp/sometenantid/{B2CSignUpSignIn}/";
        public static readonly string B2CLoginAuthorityWrongHost = $"https://anothertenantid.b2clogin.com/tfp/sometenantid/{B2CSignUpSignIn}/";
        public static readonly string B2CCustomDomain = $"https://catsareamazing.com/tfp/catsareamazing/{B2CSignUpSignIn}/";
        public static readonly string B2CLoginAuthorityUsGov = $"https://sometenantid.b2clogin.us/tfp/sometenantid/{B2CSignUpSignIn}/";
        public static readonly string B2CLoginAuthorityMoonCake = $"https://sometenantid.b2clogin.cn/tfp/sometenantid/{B2CSignUpSignIn}/";
        public static readonly string B2CLoginAuthorityBlackforest = $"https://sometenantid.b2clogin.de/tfp/sometenantid/{B2CSignUpSignIn}/";
        public static readonly string B2CSuSiHomeAccountIdentifer = $"{Uid}-{B2CSignUpSignIn}.{Utid}";
        public static readonly string B2CSuSiHomeAccountObjectId = $"{Uid}-{B2CSignUpSignIn}";
        public static readonly string B2CProfileWithDotHomeAccountIdentifer = $"{Uid}-{B2CProfileWithDot}.{Utid}";
        public static readonly string B2CProfileWithDotHomeAccountObjectId = $"{Uid}-{B2CProfileWithDot}";
        public static readonly string B2CEditProfileHomeAccountIdentifer = $"{Uid}-{B2CEditProfile}.{Utid}";
        public static readonly string B2CEditProfileHomeAccountObjectId = $"{Uid}-{B2CEditProfile}";

        public const string ClientId = "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3";
        public const string ClientId2 = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        public const string FamilyId = "1";
        public const string UniqueId = "unique_id";
        public const string IdentityProvider = "my-idp";
        public const string Name = "First Last";

        public const string Claims = @"{""userinfo"":{""given_name"":{""essential"":true},""nickname"":null,""email"":{""essential"":true},""email_verified"":{""essential"":true},""picture"":null,""http://example.info/claims/groups"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";
        public static readonly string[] ClientCapabilities = new[] { "cp1", "cp2" };
        public const string ClientCapabilitiesJson = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]}}}";
        // this a JSON merge from Claims and ClientCapabilitiesJson
        public const string ClientCapabilitiesAndClaimsJson = @"{""access_token"":{""xms_cc"":{""values"":[""cp1"",""cp2""]}},""userinfo"":{""given_name"":{""essential"":true},""nickname"":null,""email"":{""essential"":true},""email_verified"":{""essential"":true},""picture"":null,""http://example.info/claims/groups"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";

        public const string DisplayableId = "displayable@id.com";
        public const string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public const string MobileDefaultRedirectUri = "msal4a1aa1d5-c567-49d0-ad0b-cd957a47f842://auth"; // in msidentity-samples-testing tenant -> PublicClientSample
        public const string ClientSecret = "client_secret";
        public const string DefaultPassword = "password";
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
        public const string PKeyAuthResponse = "PKeyAuth Context=\"context\",Version=\"1.0\"";

        public const string RegionName = "REGION_NAME";
        public const string Region = "centralus";
        public const string InvalidRegion = "invalidregion";
        public const int TimeoutInMs = 2000;
        public const string ImdsUrl = "http://169.254.169.254/metadata/instance/compute/location";

        public const string UserAssertion = "fake_access_token";
        public const string CodeVerifier = "someCodeVerifier";

        //This value is only for testing purposes. It is for a certificate that is not used for anything other than running tests
        public const string _defaultx5cValue = @"MIIDHzCCAgegAwIBAgIQM6NFYNBJ9rdOiK+C91ZzFDANBgkqhkiG9w0BAQsFADAgMR4wHAYDVQQDExVBQ1MyQ2xpZW50Q2VydGlmaWNhdGUwHhcNMTIwNTIyMj
IxMTIyWhcNMzAwNTIyMDcwMDAwWjAgMR4wHAYDVQQDExVBQ1MyQ2xpZW50Q2VydGlmaWNhdGUwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCh7HjK
YyVMDZDT64OgtcGKWxHmK2wqzi2LJb65KxGdNfObWGxh5HQtjzrgHDkACPsgyYseqxhGxHh8I/TR6wBKx/AAKuPHE8jB4hJ1W6FczPfb7FaMV9xP0qNQrbNGZU
YbCdy7U5zIw4XrGq22l6yTqpCAh59DLufd4d7x8fCgUDV3l1ZwrncF0QrBRzns/O9Ex9pXsi2DzMa1S1PKR81D9q5QSW7LZkCgSSqI6W0b5iodx/a3RBvW3l7d
noW2fPqkZ4iMcntGNqgsSGtbXPvUR3fFdjmg+xq9FfqWyNxShlZg4U+wE1v4+kzTJxd9sgD1V0PKgW57zyzdOmTyFPJFAgMBAAGjVTBTMFEGA1UdAQRKMEiAEM
9qihCt+12P5FrjVMAEYjShIjAgMR4wHAYDVQQDExVBQ1MyQ2xpZW50Q2VydGlmaWNhdGWCEDOjRWDQSfa3ToivgvdWcxQwDQYJKoZIhvcNAQELBQADggEBAIm6
gBOkSdYjXgOvcJGgE4FJkKAMQzAhkdYq5+stfUotG6vZNL3nVOOA6aELMq/ENhrJLC3rTwLOIgj4Cy+B7BxUS9GxTPphneuZCBzjvqhzP5DmLBs8l8qu10XAsh
y1NFZmB24rMoq8C+HPOpuVLzkwBr+qcCq7ry2326auogvVMGaxhHlwSLR4Q1OhRjKs8JctCk2+5Qs1NHfawa7jWHxdAK6cLm7Rv/c0ig2Jow7wRaI5ciAcEjX7
m1t9gRT1mNeeluL4cZa6WyVXqXc6U2wfR5DY6GOMUubN5Nr1n8Czew8TPfab4OG37BuEMNmBpqoRrRgFnDzVtItOnhuFTa0=";

        public static string Defaultx5cValue
        {
            get
            {
                return Regex.Replace(_defaultx5cValue, @"\r\n?|\n", string.Empty);
            }
        }

        public const string Bearer = "Bearer";

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
        public const string MsalOBOKeyVaultUri = "https://buildautomation.vault.azure.net/secrets/IdentityDivisionDotNetOBOServiceSecret/";
        public const string MsalArlingtonOBOKeyVaultUri = "https://msidlabs.vault.azure.net:443/secrets/ARLMSIDLAB1-IDLASBS-App-CC-Secret";
        public const string FociApp1 = "https://buildautomation.vault.azure.net/secrets/automation-foci-app1/";
        public const string FociApp2 = "https://buildautomation.vault.azure.net/secrets/automation-foci-app2/";
        public const string MsalArlingtonCCAKeyVaultUri = "https://msidlabs.vault.azure.net:443/secrets/ARLMSIDLAB1-IDLASBS-App-CC-Secret";

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

        public static readonly ClientCredentialWrapper s_onPremiseCredentialWithSecret = ClientCredentialWrapper.CreateWithSecret(ClientSecret);
        public static readonly ClientCredentialWrapper s_credentialWithSecret = ClientCredentialWrapper.CreateWithSecret(ClientSecret);

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
    }
}
