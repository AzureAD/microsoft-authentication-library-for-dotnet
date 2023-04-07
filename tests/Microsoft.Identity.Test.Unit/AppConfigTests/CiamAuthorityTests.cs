﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    public class CiamAuthorityTests
    {
        private readonly string _ciamInstance = $"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}";
        private readonly string _ciamTenantGuid = "5e156ef5-9bd2-480c-9de0-d8658f21d3f7";
        private readonly string _ciamTenant = "idgciamdemo.onmicrosoft.com";

        // Possible CIAM authorities:
        // https://idgciamdemo.ciamlogin.com/idgciamdemo.onmicrosoft.com
        // https://idgciamdemo.ciamlogin.com
        // https://idgciamdemo.ciamlogin.com/5e156ef5-9bd2-480c-9de0-d8658f21d3f7
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
            string ciamAuthority = _ciamInstance + "/";
            string ciamTransformedInstance = _ciamInstance + "/";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamTransformedAuthority = _ciamInstance + "/" + ciamTenant;

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
            string ciamInstance = _ciamInstance;
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
            string ciamInstance = _ciamInstance + "/";
            string ciamTransformedInstance = _ciamInstance + "/";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamTransformedAuthority = _ciamInstance + "/" + ciamTenant;

            // Act
            CiamAuthorityHelper ciamAuthorityHelper = new CiamAuthorityHelper(ciamInstance, null);

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, ciamAuthorityHelper.TransformedAuthority.AbsoluteUri);
            Assert.AreEqual(ciamTransformedInstance, ciamAuthorityHelper.TransformedInstance);
            Assert.AreEqual(ciamTenant, ciamAuthorityHelper.TransformedTenant);
        }

        [TestMethod]
        [DataRow($"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/", $"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/idgciamdemo.onmicrosoft.com/")]
        [DataRow($"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/d57fb3d4-4b5a-4144-9328-9c1f7d58179d", $"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/d57fb3d4-4b5a-4144-9328-9c1f7d58179d/")]
        [DataRow($"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/idgciamdemo.onmicrosoft.com", $"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/idgciamdemo.onmicrosoft.com/")]
        [DataRow($"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/aDomain", $"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}/adomain/")]
        public void CiamWithAuthorityTransformationTest(string authority, string expectedAuthority)
        {
            string effectiveAuthority =
            PublicClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build()
                                                    .Authority;

            Assert.AreEqual(expectedAuthority, effectiveAuthority);
        }

        [TestMethod]
        public void CiamWithAuthorityRequestTest()
        {
            var app = 
            PublicClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                                                    .WithAuthority(_ciamInstance)
                                                    .WithDefaultRedirectUri()
                                                    .Build();

            //Ensure that CIAM authorities cannot be set when building a request
            var exception = Assert.ThrowsExceptionAsync<MsalClientException>(async () => 
            {
                await app.AcquireTokenInteractive(new[] { "someScope" })
                         .WithAuthority(_ciamInstance)
                         .ExecuteAsync()
                         .ConfigureAwait(false);
            }).Result;

            Assert.AreEqual(MsalError.SetCiamAuthorityAtRequestLevelNotSupported, exception.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.SetCiamAuthorityAtRequestLevelNotSupported, exception.Message);

            exception = Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
            {
                await app.AcquireTokenInteractive(new[] { "someScope" })
                         .WithAuthority(_ciamInstance, "idgciamdemo.onmicrosoft.com", false)
                         .ExecuteAsync()
                         .ConfigureAwait(false);
            }).Result;

            Assert.AreEqual(MsalError.SetCiamAuthorityAtRequestLevelNotSupported, exception.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.SetCiamAuthorityAtRequestLevelNotSupported, exception.Message);

            exception = Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
            {
                await app.AcquireTokenInteractive(new[] { "someScope" })
                         .WithAuthority(_ciamInstance, Guid.NewGuid(), false)
                         .ExecuteAsync()
                         .ConfigureAwait(false);
            }).Result;

            Assert.AreEqual(MsalError.SetCiamAuthorityAtRequestLevelNotSupported, exception.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.SetCiamAuthorityAtRequestLevelNotSupported, exception.Message);
        }
    }
}