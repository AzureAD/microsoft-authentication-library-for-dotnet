// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.OAuth2Tests
{
    [TestClass]
    public class ClaimsTest
    {
        [TestMethod]
        public void ClaimsMergeTest()
        {
            var mergedJson = ClaimsHelper.MergeClaimsAndClientCapabilities(
                TestConstants.Claims,
                TestConstants.ClientCapabilities);

            Assert.AreEqual(TestConstants.ClientCapabilitiesAndClaimsJson, mergedJson);
        }

        [TestMethod]
        public void ClaimsMerge_NoCapabilities_Test()
        {
            var mergedJson = ClaimsHelper.MergeClaimsAndClientCapabilities(
                TestConstants.Claims,
                new string[0]);

            Assert.AreEqual(TestConstants.Claims, mergedJson);
        }

        [TestMethod]
        public void ClaimsMerge_NoClaims_Test()
        {
            var mergedJson = ClaimsHelper.MergeClaimsAndClientCapabilities(
               null,
               TestConstants.ClientCapabilities);

            Assert.AreEqual(TestConstants.ClientCapabilitiesJson, mergedJson);
        }
    }
}
