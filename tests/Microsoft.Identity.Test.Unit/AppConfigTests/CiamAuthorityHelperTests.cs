// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    public class CiamAuthorityHelperTests
    {
        private readonly string _ciamInstance = "https://login.ciamlogin.com";
        private readonly string _ciamTenantGuid = "5e156ef5-9bd2-480c-9de0-d8658f21d3f7";
        private readonly string _ciamTenant = "idgciamdemo.onmicrosoft.com";

        // Possible CIAM authorities:
        // https://login.ciamlogin.com/idgciamdemo.onmicrosoft.com
        // https://idgciamdemo.ciamlogin.com
        // https://login.ciamlogin.com/5e156ef5-9bd2-480c-9de0-d8658f21d3f7
        [TestMethod]
        public void CiamAuthorityAdapater_WithAuthorityAndNamedTenantTest()
        {
            // Arrange
            string ciamAuthority = _ciamInstance + '/' + _ciamTenant;

            // Act
            CiamAuthorityHelper ciamAuthorityHelper = new CiamAuthorityHelper(new Uri(ciamAuthority));

            // Assert
            Assert.AreEqual(ciamAuthority, ciamAuthorityHelper.TransformedAuthority.AbsoluteUri);
            Assert.AreEqual(_ciamInstance, ciamAuthorityHelper.TransformedInstance);
            Assert.AreEqual(_ciamTenant, ciamAuthorityHelper.TransformedTenant);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithAuthorityAndTenantGUIDTest()
        {
            // Arrange
            string ciamAuthority = _ciamInstance + '/' + _ciamTenantGuid;

            // Act
            CiamAuthorityHelper ciamAuthorityHelper = new CiamAuthorityHelper(new Uri(ciamAuthority));

            // Assert
            Assert.AreEqual(ciamAuthority, ciamAuthorityHelper.TransformedAuthority.AbsoluteUri);
            Assert.AreEqual(_ciamInstance, ciamAuthorityHelper.TransformedInstance);
            Assert.AreEqual(_ciamTenantGuid, ciamAuthorityHelper.TransformedTenant);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithCiamLoginTest()
        {
            // Arrange
            string ciamAuthority = "https://idgciamdemo.ciamlogin.com/";
            string ciamTransformedInstance = "https://login.ciamlogin.com/";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamTransformedAuthority = "https://login.ciamlogin.com/" + ciamTenant;

            // Act
            CiamAuthorityHelper ciamAuthorityHelper = new CiamAuthorityHelper(new Uri(ciamAuthority));

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, ciamAuthorityHelper.TransformedAuthority.AbsoluteUri);
            Assert.AreEqual(ciamTransformedInstance, ciamAuthorityHelper.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityHelper.TransformedTenant);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithInstanceAndTenantTest()
        {
            // Arrange
            string ciamInstance = "https://idgciamdemo.ciamlogin.com";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamAuthority = ciamInstance + '/' + ciamTenant;

            // Act
            CiamAuthorityHelper ciamAuthorityHelper = new CiamAuthorityHelper(ciamInstance, ciamTenant);

            // Assert
            Assert.AreEqual(ciamAuthority, ciamAuthorityHelper.TransformedAuthority.AbsoluteUri);
            Assert.AreEqual(ciamInstance, ciamAuthorityHelper.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityHelper.TransformedTenant);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithInstanceAndNullTenantTest()
        {
            // Arrange
            string ciamInstance = "https://idgciamdemo.ciamlogin.com/";
            string ciamTransformedInstance = "https://login.ciamlogin.com/";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamTransformedAuthority = "https://login.ciamlogin.com/" + ciamTenant;

            // Act
            CiamAuthorityHelper ciamAuthorityHelper = new CiamAuthorityHelper(ciamInstance, null);

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, ciamAuthorityHelper.TransformedAuthority.AbsoluteUri);
            Assert.AreEqual(ciamTransformedInstance, ciamAuthorityHelper.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityHelper.TransformedTenant);
        }
    }
}
