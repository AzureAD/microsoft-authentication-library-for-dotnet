// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class OidcIssuerValidationTests : TestBase
    {
        #region Rule 1 - Same Scheme and Host

        [TestMethod]
        [DataRow(
            "https://login.microsoftonline.com/tenant/v2.0",
            "https://login.microsoftonline.com/tenant/v2.0",
            DisplayName = "Rule1_ExactMatch")]
        [DataRow(
            "https://login.microsoftonline.com/tenant1",
            "https://login.microsoftonline.com/tenant2/v2.0",
            DisplayName = "Rule1_SameHostDifferentPath")]
        [DataRow(
            "https://login.microsoftonline.com/tenant/",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "Rule1_TrailingSlashNormalization")]
        [DataRow(
            "https://demo.duendesoftware.com/",
            "https://demo.duendesoftware.com",
            DisplayName = "Rule1_CustomOidc_ExactMatch")]
        public void ValidateIssuer_Rule1_SameSchemeAndHost_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Rule 2 - Same Cloud (well-known issuer + well-known authority in the same cloud)

        // Pre-PR Rule 2 was a flat known-host fallback that accepted ANY known
        // Microsoft cloud host as a valid issuer regardless of which sovereign cloud
        // the configured authority pointed at. That permitted MS-to-MS cross-cloud
        // OIDC discovery acceptance (e.g. authority in Public, issuer in China).
        // Rule 2b is now narrowed: when BOTH sides are known Microsoft hosts they
        // must belong to the SAME sovereign cloud.
        [TestMethod]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.windows.net/tenant",
            DisplayName = "Rule2_Public_LoginWindowsNet")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://sts.windows.net/tenant",
            DisplayName = "Rule2_Public_StsWindowsNet")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.microsoft.com/tenant",
            DisplayName = "Rule2_Public_LoginMicrosoft")]
        [DataRow(
            "https://login.microsoftonline.us/tenant",
            "https://login.usgovcloudapi.net/tenant",
            DisplayName = "Rule2_USGov_LoginUsgovcloudapi")]
        [DataRow(
            "https://login.partner.microsoftonline.cn/tenant",
            "https://login.chinacloudapi.cn/tenant",
            DisplayName = "Rule2_China_LoginChinacloudapi")]
        public void ValidateIssuer_Rule2_KnownIssuer_SameCloud_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Rule 2a - Custom-Domain Federation with Microsoft (regression for issue #5927)

        // Documented WithOidcAuthority scenario: a customer-deployed OIDC facade
        // (custom domain) legitimately federates with Microsoft and publishes a
        // Microsoft cloud host as its issuer. Rule 2a accepts this combination.
        // Rationale: an attacker who can substitute the discovery response for a
        // custom-domain authority must already have broken transport trust for that
        // domain, at which point intercepting the token POST directly is strictly
        // easier than the cross-cloud swap. The endpoint same-cloud check in
        // OidcRetrieverWithCache deliberately does not constrain custom-domain
        // authorities for this reason.
        [TestMethod]
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            "https://login.microsoftonline.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            DisplayName = "Rule2a_OriginalUserScenario_CustomDomain_WellKnownIssuer_5927")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "Rule2a_CustomAuthority_PublicIssuer")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://sts.windows.net/tenant",
            DisplayName = "Rule2a_CustomAuthority_PublicAliasIssuer")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoftonline.us/tenant",
            DisplayName = "Rule2a_CustomAuthority_USGovIssuer")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.partner.microsoftonline.cn/tenant",
            DisplayName = "Rule2a_CustomAuthority_ChinaIssuer")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.login.microsoft.com/tenant",
            DisplayName = "Rule2a_CustomAuthority_RegionalPublicIssuer")]
        public void ValidateIssuer_Rule2a_CustomDomain_KnownIssuer_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Rule 3 - Regional Variant of Well-Known Host (Same Cloud)

        [TestMethod]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://westus2.login.microsoft.com/tenant",
            DisplayName = "Rule3_Public_Regional_LoginMicrosoft_WestUS2")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://eastus.login.microsoftonline.com/tenant",
            DisplayName = "Rule3_Public_Regional_LoginMicrosoftonline_EastUS")]
        [DataRow(
            "https://login.windows.net/tenant",
            "https://centralus.login.windows.net/tenant/v2.0",
            DisplayName = "Rule3_Public_Regional_LoginWindowsNet_CentralUS")]
        [DataRow(
            "https://login.microsoftonline.us/tenant",
            "https://usdodcentral.login.microsoftonline.us/tenant",
            DisplayName = "Rule3_USGov_Regional_USDoDCentral")]
        [DataRow(
            "https://login.partner.microsoftonline.cn/tenant",
            "https://chinaeast2.login.chinacloudapi.cn/tenant",
            DisplayName = "Rule3_China_Regional_ChinaEast2")]
        [DataRow(
            "https://login.sovcloud-identity.fr/tenant",
            "https://francecentral.login.sovcloud-identity.fr/tenant",
            DisplayName = "Rule3_Bleu_Regional")]
        [DataRow(
            "https://login.sovcloud-identity.de/tenant",
            "https://germanywestcentral.login.sovcloud-identity.de/tenant",
            DisplayName = "Rule3_Delos_Regional")]
        public void ValidateIssuer_Rule3_RegionalKnown_SameCloud_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region CIAM Validation

        [TestMethod]
        [DataRow(
            "https://customdomain.com/mytenantid",
            "https://mytenantid.ciamlogin.com/mytenantid/v2.0",
            DisplayName = "CIAM_TenantAndVersion")]
        [DataRow(
            "https://customdomain.com/mytenantid",
            "https://mytenantid.ciamlogin.com/mytenantid",
            DisplayName = "CIAM_TenantPath")]
        [DataRow(
            "https://customdomain.com/mytenantid",
            "https://mytenantid.ciamlogin.com",
            DisplayName = "CIAM_HostOnly")]
        public void ValidateIssuer_CIAM_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Cross-Cloud Rejection (covers required matrix cases 2-5)

        // Cross-cloud Microsoft-to-Microsoft must be rejected. These cases would all have been
        // accepted by the old flat known-host fallback, which was the cross-cloud OIDC discovery
        // acceptance flaw being fixed.
        [TestMethod]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.partner.microsoftonline.cn/tenant",
            DisplayName = "CrossCloud_Public_to_China")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.chinacloudapi.cn/tenant",
            DisplayName = "CrossCloud_Public_to_ChinaAlias")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.microsoftonline.us/tenant",
            DisplayName = "CrossCloud_Public_to_USGov")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.usgovcloudapi.net/tenant",
            DisplayName = "CrossCloud_Public_to_USGovAlias")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://login.microsoftonline.de/tenant",
            DisplayName = "CrossCloud_Public_to_Germany")]
        [DataRow(
            "https://login.microsoftonline.us/tenant",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "CrossCloud_USGov_to_Public")]
        [DataRow(
            "https://login.microsoftonline.us/tenant",
            "https://login.partner.microsoftonline.cn/tenant",
            DisplayName = "CrossCloud_USGov_to_China")]
        [DataRow(
            "https://login.partner.microsoftonline.cn/tenant",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "CrossCloud_China_to_Public")]
        [DataRow(
            "https://login.microsoftonline.de/tenant",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "CrossCloud_Germany_to_Public")]
        [DataRow(
            "https://login.sovcloud-identity.fr/tenant",
            "https://login.sovcloud-identity.de/tenant",
            DisplayName = "CrossCloud_Bleu_to_Delos")]
        [DataRow(
            "https://login.sovcloud-identity.sg/tenant",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "CrossCloud_GovSG_to_Public")]
        [DataRow(
            "https://login.windows-ppe.net/tenant",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "CrossCloud_PPE_to_Public")]
        // Regional variants of one cloud must not be accepted under a different cloud.
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "https://chinaeast2.login.chinacloudapi.cn/tenant",
            DisplayName = "CrossCloud_Regional_Public_to_RegionalChina")]
        [DataRow(
            "https://login.microsoftonline.us/tenant",
            "https://westus2.login.microsoft.com/tenant",
            DisplayName = "CrossCloud_Regional_USGov_to_RegionalPublic")]
        public void ValidateIssuer_CrossCloud_ShouldThrow(string authority, string issuer)
        {
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        #endregion

        #region Custom-Authority Rejection (issuer is NOT a known Microsoft host)

        // When the configured authority is a custom OIDC IdP host AND the issuer
        // is also not a known Microsoft cloud host, only Rule 1 (exact host match)
        // can accept. Any unrelated host must be rejected. (Custom authority + a
        // KNOWN Microsoft issuer is the legitimate #5927 federation case and is
        // covered by Rule 2a above.)
        [TestMethod]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://evil.example.net/tenant",
            DisplayName = "CustomAuthority_UnknownIssuer_Rejected")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://fakeb2clogin.com/tenant",
            DisplayName = "CustomAuthority_SpoofedB2CLooking_Rejected")]
        public void ValidateIssuer_CustomAuthority_NonExactIssuer_ShouldThrow(string authority, string issuer)
        {
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        #endregion

        #region Other Failing Cases

        [TestMethod]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "http://login.microsoftonline.com/tenant",
            DisplayName = "Fail_HttpScheme_WellKnown")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "http://westus2.login.microsoft.com/tenant",
            DisplayName = "Fail_HttpScheme_Regional")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.evil.example.net/tenant",
            DisplayName = "Fail_Regional_UnknownBase")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.other.example.com/tenant",
            DisplayName = "Fail_Regional_NonMatchingHost")]
        [DataRow(
            "https://myidp.example.com/tenant",
            "https://eastus.myidp.example.com/tenant",
            DisplayName = "Fail_RegionalAuthorityHost_NotSupported")]
        [DataRow(
            "https://customdomain.com/tenantA",
            "https://tenantB.ciamlogin.com/tenantB/v2.0",
            DisplayName = "CIAM_WrongTenant_Fails")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.login.evil.com/tenant",
            DisplayName = "Fail_RegionalLooking_UntrustedBase")]
        [DataRow(
            "https://otherdomain.com/tenant",
            "https://eastus.myidp.example.com/tenant",
            DisplayName = "Fail_RegionalLooking_WrongAuthorityBase")]
        public void ValidateIssuer_ShouldThrow(string authority, string issuer)
        {
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        [TestMethod]
        public void ValidateIssuer_NullIssuer_Fails()
        {
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"), null));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        [TestMethod]
        public void ValidateIssuer_EmptyIssuer_Fails()
        {
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"), ""));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        #endregion

        #region Behavior Matrix (single-source-of-truth: Before vs. After)

        // This region documents every distinguishable (authority shape, issuer shape)
        // case in one place, with the BEFORE behavior (PR #5931 - flat known-host
        // fallback) and the AFTER behavior (this PR - two-pronged Rule 2 plus the
        // retrieval-time endpoint same-cloud check in OidcRetrieverWithCache).
        // Each row is exercised here to make any future regression visible at a glance.
        //
        // Legend:
        //   AUTH      : custom = unknown host;       msPublic = login.microsoftonline.com (Public alias);
        //               msUSGov = login.microsoftonline.us; msChina = login.partner.microsoftonline.cn;
        //               msRegion = westus2.login.microsoft.com (regional Public).
        //   ISSUER    : same shorthand.
        //   Before    : pass / throw under the OLD flat known-host fallback.
        //   After     : pass / throw under THIS PR (column under test).
        //   Why-after : which rule (1 / 2a / 2b / 3) accepted, or why we now reject.

        // ----- ROWS THAT PASS BEFORE AND AFTER (no behavior change) -----

        [TestMethod]
        // # | AUTH               | ISSUER                  | Before | After | Rule
        // 1 | msPublic/contoso   | msPublic/contoso/v2.0   | pass   | pass  | Rule 1 exact host
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "https://login.microsoftonline.com/contoso/v2.0",
            DisplayName = "Matrix_01_Pass_BeforeAndAfter_Rule1_PublicExact")]
        // 2 | custom/x           | custom/y                | pass   | pass  | Rule 1 exact host (custom)
        [DataRow(
            "https://demo.duendesoftware.com/x",
            "https://demo.duendesoftware.com/y",
            DisplayName = "Matrix_02_Pass_BeforeAndAfter_Rule1_CustomExact")]
        // 3 | msPublic/contoso   | sts.windows.net/contoso | pass   | pass  | Rule 2b same Public cloud
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "https://sts.windows.net/contoso",
            DisplayName = "Matrix_03_Pass_BeforeAndAfter_Rule2b_PublicAlias")]
        // 4 | msPublic/contoso   | msRegion/contoso        | pass   | pass  | Rule 2b same Public cloud (regional variant)
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "https://westus2.login.microsoft.com/contoso",
            DisplayName = "Matrix_04_Pass_BeforeAndAfter_Rule2b_PublicRegional")]
        // 5 | msUSGov/contoso    | login.usgovcloudapi/c   | pass   | pass  | Rule 2b same US Gov cloud
        [DataRow(
            "https://login.microsoftonline.us/contoso",
            "https://login.usgovcloudapi.net/contoso",
            DisplayName = "Matrix_05_Pass_BeforeAndAfter_Rule2b_USGovAlias")]
        // 6 | msChina/contoso    | chinacloudapi.cn/c      | pass   | pass  | Rule 2b same China cloud
        [DataRow(
            "https://login.partner.microsoftonline.cn/contoso",
            "https://login.chinacloudapi.cn/contoso",
            DisplayName = "Matrix_06_Pass_BeforeAndAfter_Rule2b_ChinaAlias")]
        // 7 | customdomain/x     | x.ciamlogin.com/x       | pass   | pass  | Rule 3 CIAM tenant pattern
        [DataRow(
            "https://customdomain.com/contoso",
            "https://contoso.ciamlogin.com/contoso/v2.0",
            DisplayName = "Matrix_07_Pass_BeforeAndAfter_Rule3_CIAM")]
        // 8 (#5927) | custom/tid | msPublic/tid            | pass   | pass  | Rule 2a custom-domain federation
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            "https://login.microsoftonline.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            DisplayName = "Matrix_08_Pass_BeforeAndAfter_Rule2a_5927_CustomDomainFederation")]
        public void Matrix_PassBeforeAndAfter(string authority, string issuer)
        {
            // Both old and new behavior accept these. They must continue to pass.
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        // ----- ROWS THAT PASSED BEFORE BUT NOW THROW (the actual fix) -----

        [TestMethod]
        // #  | AUTH              | ISSUER                       | Before | After | Why now
        // 9  | msPublic/c        | msChina/c                    | pass   | THROW | Rule 2b same-cloud rejects (cross-cloud)
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "https://login.partner.microsoftonline.cn/contoso",
            DisplayName = "Matrix_09_Throw_PassBefore_CrossCloud_Public_China")]
        // 10 | msPublic/c        | msUSGov/c                    | pass   | THROW | Rule 2b same-cloud rejects
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "https://login.microsoftonline.us/contoso",
            DisplayName = "Matrix_10_Throw_PassBefore_CrossCloud_Public_USGov")]
        // 11 | msUSGov/c         | msPublic/c                   | pass   | THROW | Rule 2b same-cloud rejects
        [DataRow(
            "https://login.microsoftonline.us/contoso",
            "https://login.microsoftonline.com/contoso",
            DisplayName = "Matrix_11_Throw_PassBefore_CrossCloud_USGov_Public")]
        // 12 | msChina/c         | msPublic/c                   | pass   | THROW | Rule 2b same-cloud rejects
        [DataRow(
            "https://login.partner.microsoftonline.cn/contoso",
            "https://login.microsoftonline.com/contoso",
            DisplayName = "Matrix_12_Throw_PassBefore_CrossCloud_China_Public")]
        // 13 | msPublic/c        | regionalChina/c              | pass   | THROW | Rule 2b same-cloud rejects (regional cross-cloud)
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "https://chinaeast2.login.chinacloudapi.cn/contoso",
            DisplayName = "Matrix_13_Throw_PassBefore_CrossCloud_Public_RegionalChina")]
        // 14 | msUSGov/c         | regionalPublic/c             | pass   | THROW | Rule 2b same-cloud rejects (regional cross-cloud)
        [DataRow(
            "https://login.microsoftonline.us/contoso",
            "https://westus2.login.microsoft.com/contoso",
            DisplayName = "Matrix_14_Throw_PassBefore_CrossCloud_USGov_RegionalPublic")]
        // 15 | bleu/c            | delos/c                      | pass   | THROW | Rule 2b same-cloud rejects (sovereign-to-sovereign)
        [DataRow(
            "https://login.sovcloud-identity.fr/contoso",
            "https://login.sovcloud-identity.de/contoso",
            DisplayName = "Matrix_15_Throw_PassBefore_CrossCloud_Bleu_Delos")]
        public void Matrix_PassedBefore_NowThrows_CrossCloud(string authority, string issuer)
        {
            // BEFORE this PR: these all wrongly passed via the flat known-host fallback.
            // AFTER this PR: Rule 2b (MS authority + MS issuer => MUST be same cloud)
            // rejects every cross-cloud combination.
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        // ----- ROWS THAT THROW BEFORE AND AFTER (no behavior change) -----

        [TestMethod]
        // #  | AUTH       | ISSUER                            | Before | After | Why
        // 16 | msPublic/c | http://msPublic/c                 | throw  | throw | non-HTTPS issuer
        [DataRow(
            "https://login.microsoftonline.com/contoso",
            "http://login.microsoftonline.com/contoso",
            DisplayName = "Matrix_16_Throw_BeforeAndAfter_HttpScheme")]
        // 17 | custom/c   | evil.example.net/c                | throw  | throw | unknown issuer + custom authority
        [DataRow(
            "https://custom.example.com/contoso",
            "https://evil.example.net/contoso",
            DisplayName = "Matrix_17_Throw_BeforeAndAfter_UnknownIssuer_CustomAuthority")]
        // 18 | custom/A   | tenantB.ciamlogin.com/B/v2.0      | throw  | throw | CIAM tenant mismatch
        [DataRow(
            "https://customdomain.com/tenantA",
            "https://tenantB.ciamlogin.com/tenantB/v2.0",
            DisplayName = "Matrix_18_Throw_BeforeAndAfter_CIAM_TenantMismatch")]
        // 19 | custom/c   | attacker.evil.login.microsoft.com | throw  | throw | regional shape stops at first dot, base is not a known host
        [DataRow(
            "https://custom.example.com/contoso",
            "https://attacker.evil.login.microsoft.com/contoso",
            DisplayName = "Matrix_19_Throw_BeforeAndAfter_MultiLabelRegionalLooking")]
        public void Matrix_ThrowBeforeAndAfter(string authority, string issuer)
        {
            // Both old and new behavior reject these. They must continue to throw.
            var ex = AssertException.Throws<MsalServiceException>(
                () => OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        // ----- ROWS THAT THREW BEFORE BUT NOW PASS (only #5927 falls here) -----

        [TestMethod]
        // The only case in this category is the #5927 regression set: a custom-domain
        // authority federating with a known Microsoft issuer. The pre-#5931 library
        // threw on these (issuer != authority host) and the operator workaround was to
        // configure login.microsoftonline.com directly, losing the customer's branded
        // domain. PR #5931 made these pass via the flat known-host fallback (which
        // also opened the cross-cloud hole). This PR keeps them passing via Rule 2a
        // while closing the cross-cloud hole at Rule 2b plus the retrieval-time
        // endpoint same-cloud check in OidcRetrieverWithCache.
        // #  | AUTH                                | ISSUER                | Before-#5931 | After this PR | Rule
        // 20 | parentpay-style custom domain       | login.microsoftonline | throw        | pass          | Rule 2a custom-domain federation
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            "https://login.microsoftonline.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            DisplayName = "Matrix_20_Pass_FormerlyThrew_5927_ParentPay")]
        // 21 | generic custom authority            | sts.windows.net       | throw        | pass          | Rule 2a (Public alias under custom domain)
        [DataRow(
            "https://custom.example.com/tenant",
            "https://sts.windows.net/tenant",
            DisplayName = "Matrix_21_Pass_FormerlyThrew_CustomAuthority_PublicAlias")]
        // 22 | generic custom authority            | regional Public       | throw        | pass          | Rule 2a (Public regional under custom domain)
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.login.microsoft.com/tenant",
            DisplayName = "Matrix_22_Pass_FormerlyThrew_CustomAuthority_RegionalPublic")]
        public void Matrix_FormerlyThrew_NowPass_CustomDomainFederation(string authority, string issuer)
        {
            // Rule 2a is the single rule that lights up here.
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region KnownMetadataProvider.AreInSameCloud helper

        [TestMethod]
        [DataRow("login.microsoftonline.com", "login.windows.net", DisplayName = "SameCloud_Public_PublicAlias")]
        [DataRow("login.microsoftonline.com", "westus2.login.microsoft.com", DisplayName = "SameCloud_Public_RegionalPublic")]
        [DataRow("login.microsoftonline.us", "login.usgovcloudapi.net", DisplayName = "SameCloud_USGov_USGovAlias")]
        [DataRow("login.partner.microsoftonline.cn", "login.chinacloudapi.cn", DisplayName = "SameCloud_China_ChinaAlias")]
        [DataRow("login.partner.microsoftonline.cn", "chinaeast2.login.chinacloudapi.cn", DisplayName = "SameCloud_China_RegionalChina")]
        [DataRow("login.sovcloud-identity.fr", "francecentral.login.sovcloud-identity.fr", DisplayName = "SameCloud_Bleu_Regional")]
        public void AreInSameCloud_ReturnsTrue_ForSameCloud(string hostA, string hostB)
        {
            Assert.IsTrue(KnownMetadataProvider.AreInSameCloud(hostA, hostB));
            Assert.IsTrue(KnownMetadataProvider.AreInSameCloud(hostB, hostA));
        }

        [TestMethod]
        [DataRow("login.microsoftonline.com", "login.partner.microsoftonline.cn", DisplayName = "DifferentCloud_Public_China")]
        [DataRow("login.microsoftonline.com", "login.microsoftonline.us", DisplayName = "DifferentCloud_Public_USGov")]
        [DataRow("login.microsoftonline.us", "login.chinacloudapi.cn", DisplayName = "DifferentCloud_USGov_China")]
        [DataRow("login.microsoftonline.com", "login.microsoftonline.de", DisplayName = "DifferentCloud_Public_Germany")]
        [DataRow("login.sovcloud-identity.fr", "login.sovcloud-identity.de", DisplayName = "DifferentCloud_Bleu_Delos")]
        [DataRow("login.windows-ppe.net", "login.microsoftonline.com", DisplayName = "DifferentCloud_PPE_Public")]
        // Unknown / custom hosts never match anything (default-deny).
        [DataRow("login.microsoftonline.com", "custom.example.com", DisplayName = "Unknown_Public_Custom")]
        [DataRow("custom.example.com", "another.example.org", DisplayName = "Unknown_Custom_Custom")]
        [DataRow("custom.example.com", null, DisplayName = "Unknown_Null")]
        [DataRow(null, "login.microsoftonline.com", DisplayName = "Null_Public")]
        // Regional-prefix shape constraint: only lowercase alphanumeric (with optional
        // internal hyphens) DNS-label-shaped prefixes (max 63 chars per RFC 1035) are
        // accepted as regional variants. Anything else must NOT resolve to the base cloud entry.
        [DataRow("login.microsoftonline.com", "attacker.evil.login.microsoft.com", DisplayName = "RegionalPrefix_MultiLabel_Rejected")]
        [DataRow("login.microsoftonline.com", "weird_prefix.login.microsoft.com", DisplayName = "RegionalPrefix_Underscore_Rejected")]
        [DataRow("login.microsoftonline.com", "PREFIX.login.microsoft.com", DisplayName = "RegionalPrefix_Uppercase_Rejected")]
        [DataRow("login.microsoftonline.com", "-leading.login.microsoft.com", DisplayName = "RegionalPrefix_LeadingHyphen_Rejected")]
        [DataRow("login.microsoftonline.com", "trailing-.login.microsoft.com", DisplayName = "RegionalPrefix_TrailingHyphen_Rejected")]
        // 64 chars (one over the RFC 1035 DNS-label cap of 63).
        [DataRow("login.microsoftonline.com", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.login.microsoft.com", DisplayName = "RegionalPrefix_TooLong_Rejected")]
        public void AreInSameCloud_ReturnsFalse_ForDifferentOrUnknown(string hostA, string hostB)
        {
            Assert.IsFalse(KnownMetadataProvider.AreInSameCloud(hostA, hostB));
        }

        [TestMethod]
        // Positive coverage for region prefixes that the constraint must continue to allow.
        // Real Azure regions, including hyphenated forms and the longest published names.
        [DataRow("westus2", DisplayName = "RegionalPrefix_Westus2")]
        [DataRow("eastus2euap", DisplayName = "RegionalPrefix_Eastus2Euap")]
        [DataRow("chinaeast2", DisplayName = "RegionalPrefix_ChinaEast2")]
        [DataRow("usgovvirginia", DisplayName = "RegionalPrefix_UsGovVirginia")]
        [DataRow("francecentral", DisplayName = "RegionalPrefix_FranceCentral")]
        [DataRow("southafricanorth", DisplayName = "RegionalPrefix_SouthAfricaNorth")]
        public void AreInSameCloud_AcceptsKnownRegionPrefixes(string regionPrefix)
        {
            string regionalHost = regionPrefix + ".login.microsoft.com";
            Assert.IsTrue(KnownMetadataProvider.AreInSameCloud(regionalHost, "login.microsoftonline.com"));
        }

        #endregion
    }
}
