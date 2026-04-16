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
        #region Passing Tests

        [TestMethod]
        [DataRow(
            "https://login.microsoftonline.com/tenant/v2.0",
            "https://login.microsoftonline.com/tenant/v2.0",
            DisplayName = "1 - ExactSchemeAndHostMatch")]
        [DataRow(
            "https://login.microsoftonline.com/tenant1",
            "https://login.microsoftonline.com/tenant2/v2.0",
            DisplayName = "2 - SameHostDifferentPath")]
        [DataRow(
            "https://clientlogin.test.parentpay.com/ebdf0e4c/v2.0",
            "https://login.microsoftonline.com/ebdf0e4c/v2.0",
            DisplayName = "3 - WellKnownIssuer_CustomDomainAuthority")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.windows.net/tenant",
            DisplayName = "4 - WellKnownIssuer_LoginWindowsNet")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://sts.windows.net/tenant",
            DisplayName = "5 - WellKnownIssuer_StsWindowsNet")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.microsoftonline.us/tenant",
            DisplayName = "6 - WellKnownIssuer_USGov")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://login.chinacloudapi.cn/tenant",
            DisplayName = "7 - WellKnownIssuer_China")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://westus2.login.microsoft.com/tenant",
            DisplayName = "8 - RegionalVariant_WellKnownBase")]
        [DataRow(
            "https://myidp.example.com/tenant",
            "https://eastus.myidp.example.com/tenant",
            DisplayName = "9 - RegionalVariant_AuthorityHost")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://mytenant.b2clogin.com/tenant",
            DisplayName = "10 - B2C_ValidSubdomain")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://mytenant.ciamlogin.com/tenant",
            DisplayName = "11 - B2C_CiamLogin")]
        [DataRow(
            "https://customdomain.com/mytenantid",
            "https://mytenantid.ciamlogin.com/mytenantid/v2.0",
            DisplayName = "12 - CIAM_HostSuffixWithTenantAndVersion")]
        [DataRow(
            "https://customdomain.com/mytenantid",
            "https://mytenantid.ciamlogin.com/mytenantid",
            DisplayName = "13 - CIAM_HostSuffixWithTenantPath")]
        [DataRow(
            "https://customdomain.com/mytenantid",
            "https://mytenantid.ciamlogin.com",
            DisplayName = "14 - CIAM_HostSuffixHostOnly")]
        [DataRow(
            "https://login.microsoftonline.com/tenant/",
            "https://login.microsoftonline.com/tenant",
            DisplayName = "20 - TrailingSlashNormalization")]
        public void ValidateIssuer_ShouldPass(string authority, string issuer)
        {
            // Should not throw
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Failing Tests

        [TestMethod]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://evil.example.net/tenant",
            DisplayName = "15 - UnknownIssuer_Fails")]
        [DataRow(
            "https://custom.example.com/tenant",
            "https://fakeb2clogin.com/tenant",
            DisplayName = "16 - SpoofedB2C_NoDotPrefix_Fails")]
        [DataRow(
            "https://login.microsoftonline.com/tenant",
            "http://login.microsoftonline.com/tenant",
            DisplayName = "19 - HttpScheme_Fails")]
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

        #region Additional Well-Known Host Coverage

        [TestMethod]
        [DataRow("https://login.microsoft.com/tenant", DisplayName = "login.microsoft.com")]
        [DataRow("https://login.partner.microsoftonline.cn/tenant", DisplayName = "login.partner.microsoftonline.cn")]
        [DataRow("https://login.microsoftonline.de/tenant", DisplayName = "login.microsoftonline.de")]
        [DataRow("https://login-us.microsoftonline.com/tenant", DisplayName = "login-us.microsoftonline.com")]
        [DataRow("https://login.usgovcloudapi.net/tenant", DisplayName = "login.usgovcloudapi.net")]
        [DataRow("https://login.sovcloud-identity.fr/tenant", DisplayName = "login.sovcloud-identity.fr")]
        [DataRow("https://login.sovcloud-identity.de/tenant", DisplayName = "login.sovcloud-identity.de")]
        [DataRow("https://login.sovcloud-identity.sg/tenant", DisplayName = "login.sovcloud-identity.sg")]
        public void ValidateIssuer_AllWellKnownHosts_ShouldPass(string issuer)
        {
            // A custom domain authority should accept any well-known issuer
            OidcRetrieverWithCache.ValidateIssuer(
                new Uri("https://custom.example.com/tenant"), issuer);
        }

        #endregion

        #region Additional B2C Suffix Coverage

        [TestMethod]
        [DataRow("https://tenant.b2clogin.cn/tenant", DisplayName = ".b2clogin.cn")]
        [DataRow("https://tenant.b2clogin.us/tenant", DisplayName = ".b2clogin.us")]
        [DataRow("https://tenant.b2clogin.de/tenant", DisplayName = ".b2clogin.de")]
        public void ValidateIssuer_AllB2CSuffixes_ShouldPass(string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(
                new Uri("https://custom.example.com/tenant"), issuer);
        }

        #endregion
    }
}
