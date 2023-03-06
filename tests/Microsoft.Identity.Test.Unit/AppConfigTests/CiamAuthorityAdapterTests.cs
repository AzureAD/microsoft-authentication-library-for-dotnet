// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Graphics.Printing3D;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    public class CiamAuthorityAdapterTests
    {
        private readonly string _transformedMetadata = "{\"api-version\": \"1.1\",\"metadata\": [{\"preferred_network\": \"login.windows.net\",\"preferred_cache\": \"login.windows.net\",\"aliases\": [\"login.windows.net\",\"login.ciamlogin.com\"]}]}";
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
            CiamAuthorityAdapter ciamAuthorityAdapter = new CiamAuthorityAdapter(ciamAuthority);

            // Assert
            Assert.AreEqual(ciamAuthority, ciamAuthorityAdapter.TransformedAuthority);
            Assert.AreEqual(_transformedMetadata, ciamAuthorityAdapter.TransformedMetadata);
            Assert.AreEqual(_ciamInstance, ciamAuthorityAdapter.TransformedInstance);
            Assert.AreEqual(_ciamTenant, ciamAuthorityAdapter.TransformedTenant);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithAuthorityAndTenantGUIDTest()
        {
            // Arrange
            string ciamAuthority = _ciamInstance + '/' + _ciamTenantGuid;

            // Act
            CiamAuthorityAdapter ciamAuthorityAdapter = new CiamAuthorityAdapter(ciamAuthority);

            // Assert
            Assert.AreEqual(ciamAuthority, ciamAuthorityAdapter.TransformedAuthority);
            Assert.AreEqual(_transformedMetadata, ciamAuthorityAdapter.TransformedMetadata);
            Assert.AreEqual(_ciamInstance, ciamAuthorityAdapter.TransformedInstance);
            Assert.AreEqual(_ciamTenantGuid, ciamAuthorityAdapter.TransformedTenant);
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
            CiamAuthorityAdapter ciamAuthorityAdapter = new CiamAuthorityAdapter(ciamAuthority);

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, ciamAuthorityAdapter.TransformedAuthority);
            Assert.AreEqual(_transformedMetadata, ciamAuthorityAdapter.TransformedMetadata);
            Assert.AreEqual(ciamTransformedInstance, ciamAuthorityAdapter.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityAdapter.TransformedTenant);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithInstanceAndTenantTest()
        {
            // Arrange
            string ciamInstance = "https://idgciamdemo.ciamlogin.com";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamAuthority = ciamInstance + '/' + ciamTenant;

            // Act
            CiamAuthorityAdapter ciamAuthorityAdapter = new CiamAuthorityAdapter(ciamInstance, ciamTenant);

            // Assert
            Assert.AreEqual(ciamAuthority, ciamAuthorityAdapter.TransformedAuthority);
            Assert.AreEqual(_transformedMetadata, ciamAuthorityAdapter.TransformedMetadata);
            Assert.AreEqual(ciamInstance, ciamAuthorityAdapter.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityAdapter.TransformedTenant);
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
            CiamAuthorityAdapter ciamAuthorityAdapter = new CiamAuthorityAdapter(ciamInstance, null);

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, ciamAuthorityAdapter.TransformedAuthority);
            Assert.AreEqual(_transformedMetadata, ciamAuthorityAdapter.TransformedMetadata);
            Assert.AreEqual(ciamTransformedInstance, ciamAuthorityAdapter.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityAdapter.TransformedTenant);
        }
    }
}
