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
    public class OidcIssuerValidationTests
    {
        #region Passing cases

        [TestMethod]
        [DataRow("https://login.microsoftonline.com/tenant/v2.0", "https://login.microsoftonline.com/tenant/v2.0", DisplayName = "ExactSchemeAndHostMatch")]
        [DataRow("https://login.microsoftonline.com/tenant1", "https://login.microsoftonline.com/tenant2/v2.0", DisplayName = "SameHostDifferentPath")]
        [DataRow("https://login.microsoftonline.com/tenant/", "https://login.microsoftonline.com/tenant", DisplayName = "TrailingSlashNormalization")]
        public void ValidateIssuer_SchemeAndHostMatch_Passes(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        [TestMethod]
        [DataRow("https://clientlogin.test.parentpay.com/ebdf0e4c/v2.0", "https://login.microsoftonline.com/ebdf0e4c/v2.0", DisplayName = "WellKnownIssuer_CustomDomainAuthority")]
        [DataRow("https://custom.example.com/tenant", "https://login.windows.net/tenant", DisplayName = "WellKnownIssuer_LoginWindowsNet")]
        [DataRow("https://custom.example.com/tenant", "https://sts.windows.net/tenant", DisplayName = "WellKnownIssuer_StsWindowsNet")]
        [DataRow("https://custom.example.com/tenant", "https://login.microsoftonline.us/tenant", DisplayName = "WellKnownIssuer_USGov")]
        [DataRow("https://custom.example.com/tenant", "https://login.chinacloudapi.cn/tenant", DisplayName = "WellKnownIssuer_China")]
        public void ValidateIssuer_WellKnownHost_Passes(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        [TestMethod]
        [DataRow("https://custom.example.com/tenant", "https://westus2.login.microsoft.com/tenant", DisplayName = "RegionalVariant_WellKnownBase")]
        [DataRow("https://myidp.example.com/tenant", "https://eastus.myidp.example.com/tenant", DisplayName = "RegionalVariant_AuthorityHost")]
        public void ValidateIssuer_RegionalVariant_Passes(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        [TestMethod]
        [DataRow("https://custom.example.com/tenant", "https://mytenant.b2clogin.com/tenant", DisplayName = "B2C_ValidSubdomain")]
        [DataRow("https://custom.example.com/tenant", "https://mytenant.ciamlogin.com/tenant", DisplayName = "B2C_CiamLogin")]
        public void ValidateIssuer_B2CHostSuffix_Passes(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        [TestMethod]
        [DataRow("https://customdomain.com/mytenantid", "https://mytenantid.ciamlogin.com/mytenantid/v2.0", DisplayName = "CIAM_TenantPattern")]
        [DataRow("https://customdomain.com/mytenantid", "https://mytenantid.ciamlogin.com/mytenantid", DisplayName = "CIAM_TenantPatternNoVersion")]
        [DataRow("https://customdomain.com/mytenantid", "https://mytenantid.ciamlogin.com", DisplayName = "CIAM_TenantPatternHostOnly")]
        public void ValidateIssuer_CIAMTenantPattern_Passes(string authority, string issuer)
        {
            OidcRetrieverWithCache.ValidateIssuer(new Uri(authority), issuer);
        }

        #endregion

        #region Failing cases

        [TestMethod]
        public void ValidateIssuer_UnknownIssuer_Throws()
        {
            var ex = AssertException.Throws<MsalServiceException>(() =>
                OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"),
                    "https://evil.example.net/tenant"));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        [TestMethod]
        public void ValidateIssuer_SpoofedB2C_NoDotPrefix_Throws()
        {
            var ex = AssertException.Throws<MsalServiceException>(() =>
                OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"),
                    "https://fakeb2clogin.com/tenant"));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        [TestMethod]
        public void ValidateIssuer_NullIssuer_Throws()
        {
            var ex = AssertException.Throws<MsalServiceException>(() =>
                OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"),
                    null));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        [TestMethod]
        public void ValidateIssuer_EmptyIssuer_Throws()
        {
            var ex = AssertException.Throws<MsalServiceException>(() =>
                OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"),
                    ""));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        [TestMethod]
        public void ValidateIssuer_HttpScheme_WellKnownHost_Passes()
        {
            // http scheme with a well-known host is accepted because the host is trusted
            // (matches Python MSAL behavior where well-known host check doesn't verify scheme)
            OidcRetrieverWithCache.ValidateIssuer(
                new Uri("https://login.microsoftonline.com/tenant"),
                "http://login.microsoftonline.com/tenant");
        }

        [TestMethod]
        public void ValidateIssuer_HttpScheme_UnknownHost_Throws()
        {
            // http scheme with an unknown host should fail (scheme mismatch + not well-known)
            var ex = AssertException.Throws<MsalServiceException>(() =>
                OidcRetrieverWithCache.ValidateIssuer(
                    new Uri("https://custom.example.com/tenant"),
                    "http://custom.example.com/tenant"));

            Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
        }

        #endregion
    }
}
