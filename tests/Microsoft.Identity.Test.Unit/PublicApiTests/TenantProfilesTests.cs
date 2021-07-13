// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TenantProfilesTests
    {
        // Some tests load the TokenCache from a file and use this clientId
        private const string ClientIdInFile = "0615b6ca-88d4-4884-8729-b178178f7c27";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void Constructor_ClaimsArePopulatedForIdToken()
        {
            string idToken = MockHelpers.CreateIdToken("oid", "some_name", TestConstants.Utid);
            TenantProfile tenantProfile = new TenantProfile("oid", TestConstants.Utid, IdToken.Parse(idToken).ClaimsPrincipal, true);

            var claims = tenantProfile.ClaimsPrincipal.Claims;

            Assert.AreEqual(12, claims.Count());
            Assert.AreEqual("oid", claims.FirstOrDefault(claim => claim.Type.Equals("oid")).Value);
            Assert.AreEqual("some_name", claims.FirstOrDefault(claim => claim.Type.Equals("preferred_username")).Value);
            Assert.AreEqual(TestConstants.Utid, claims.FirstOrDefault(claim => claim.Type.Equals("tid")).Value);
        }

        [TestMethod]
        public void Constructor_ClaimsNullForNullIdToken()
        {
            TenantProfile tenantProfile = new TenantProfile("oid", TestConstants.Utid, null, false);

            Assert.IsNull(tenantProfile.ClaimsPrincipal);
        }

       
    }
}
