// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
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
        public void ValidateIssuer_Rule1_SameSchemeAndHost_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Rule 2 - Well-Known Microsoft Authority Host

        [TestMethod]
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            "https://login.microsoftonline.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            DisplayName = "Rule2_OriginalUserScenario_CustomDomain_WellKnownIssuer")]
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c/v2.0",
            "https://login.microsoftonline.com/ebdf0e4c/v2.0",
            DisplayName = "Rule2_CustomDomain_WellKnownIssuer")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.windows.net/tenant",
            DisplayName = "Rule2_WellKnown_LoginWindowsNet")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://sts.windows.net/tenant",
            DisplayName = "Rule2_WellKnown_StsWindowsNet")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoftonline.us/tenant",
            DisplayName = "Rule2_WellKnown_USGov")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.chinacloudapi.cn/tenant",
            DisplayName = "Rule2_WellKnown_China")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoft.com/tenant",
            DisplayName = "Rule2_WellKnown_LoginMicrosoft")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.partner.microsoftonline.cn/tenant",
            DisplayName = "Rule2_WellKnown_ChinaPartner")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoftonline.de/tenant",
            DisplayName = "Rule2_WellKnown_Germany")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login-us.microsoftonline.com/tenant",
            DisplayName = "Rule2_WellKnown_LoginUS")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.usgovcloudapi.net/tenant",
            DisplayName = "Rule2_WellKnown_USGovApi")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.sovcloud-identity.fr/tenant",
            DisplayName = "Rule2_WellKnown_BleuCloud")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.sovcloud-identity.de/tenant",
            DisplayName = "Rule2_WellKnown_DelosCloud")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.sovcloud-identity.sg/tenant",
            DisplayName = "Rule2_WellKnown_GovSG")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.windows-ppe.net/tenant",
            DisplayName = "Rule2_WellKnown_LoginWindowsPpe")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://sts.windows-ppe.net/tenant",
            DisplayName = "Rule2_WellKnown_StsWindowsPpe")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoft-ppe.com/tenant",
            DisplayName = "Rule2_WellKnown_LoginMicrosoftPpe")]
        public void ValidateIssuer_Rule2_WellKnownHost_ShouldPass(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Rule 3 - Regional Variant of Well-Known Host

        [TestMethod]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.login.microsoft.com/tenant",
            DisplayName = "Rule3_Regional_LoginMicrosoft_WestUS2")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://eastus.login.microsoftonline.com/tenant",
            DisplayName = "Rule3_Regional_LoginMicrosoftonline_EastUS")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://centralus.login.windows.net/tenant/v2.0",
            DisplayName = "Rule3_Regional_LoginWindowsNet_CentralUS")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westeurope.sts.windows.net/tenant",
            DisplayName = "Rule3_Regional_StsWindowsNet_WestEurope")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://usdodcentral.login.microsoftonline.us/tenant",
            DisplayName = "Rule3_Regional_USGov_USDoDCentral")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://chinaeast2.login.chinacloudapi.cn/tenant",
            DisplayName = "Rule3_Regional_China_ChinaEast2")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://francecentral.login.sovcloud-identity.fr/tenant",
            DisplayName = "Rule3_Regional_BleuCloud")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://germanywestcentral.login.sovcloud-identity.de/tenant",
            DisplayName = "Rule3_Regional_DelosCloud")]
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            "https://westeurope.login.microsoftonline.com/ebdf0e4c-ebe2-4793-af52-ceaf96f82741/v2.0",
            DisplayName = "Rule3_OriginalUserScenario_Regional_CustomDomain_WellKnown")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.login.windows-ppe.net/tenant",
            DisplayName = "Rule3_Regional_LoginWindowsPpe")]
        public void ValidateIssuer_Rule3_RegionalWellKnown_ShouldPass(string authority, string issuer)
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

        #region Failing Tests

        [TestMethod]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://evil.example.net/tenant",
            DisplayName = "Fail_UnknownIssuer")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://fakeb2clogin.com/tenant",
            DisplayName = "Fail_SpoofedB2C_NoDotPrefix")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "http://login.microsoftonline.com/tenant",
            DisplayName = "Fail_HttpScheme_WellKnown")]
        [DataRow(
            "https://custom.example.com/tenant",
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
    }
}
