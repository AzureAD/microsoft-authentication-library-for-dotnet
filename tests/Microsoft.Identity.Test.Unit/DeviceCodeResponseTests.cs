﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class DeviceCodeResponseTests : TestBase
    {
        private const string VerificationUrl = "http://verification.url";
        private const string VerificationUri = "http://verification.uri";

        [TestMethod]
        public void DeviceCodeResultShouldContainVerificationUrlIfOnlyThatIsPresent()
        {
            var deviceCodeResponse = new DeviceCodeResponse { VerificationUrl = VerificationUrl };
            var deviceCodeResult = deviceCodeResponse.GetResult(TestConstants.ClientId, TestConstants.s_scope);

            Assert.AreEqual(VerificationUrl, deviceCodeResult.VerificationUrl);
        }

        [TestMethod]
        public void DeviceCodeResultShouldContainVerificationUriIfOnlyThatIsPresent()
        {
            var deviceCodeResponse = new DeviceCodeResponse { VerificationUri = VerificationUri };
            var deviceCodeResult = deviceCodeResponse.GetResult(TestConstants.ClientId, TestConstants.s_scope);

            Assert.AreEqual(VerificationUri, deviceCodeResult.VerificationUrl);
        }

        [TestMethod]
        public void DeviceCodeResultShouldContainVerificationUriIfBothArePresent()
        {
            var deviceCodeResponse = new DeviceCodeResponse {
                VerificationUri = VerificationUri,
                VerificationUrl = VerificationUrl
            };

            var deviceCodeResult = deviceCodeResponse.GetResult(TestConstants.ClientId, TestConstants.s_scope);

            Assert.AreEqual(VerificationUri, deviceCodeResult.VerificationUrl);
        }
    }
}
