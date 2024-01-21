// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Drawing.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory(TestCategories.BuilderTests)]
    public class ManagedIdentityApplicationBuilderTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void TestConstructor()
        {
            var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .BuildConcrete();

            // Assert defaults
            Assert.AreEqual(Constants.ManagedIdentityDefaultClientId, mi.ServiceBundle.Config.ClientId);
            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, mi.ServiceBundle.Config.RedirectUri);
            Assert.AreEqual(Constants.ManagedIdentityDefaultTenant, mi.ServiceBundle.Config.TenantId);

            Assert.IsNotNull(mi.ServiceBundle.Config.ClientName);
            Assert.IsNotNull(mi.ServiceBundle.Config.ClientVersion);

            Assert.IsNull(mi.ServiceBundle.Config.HttpClientFactory);
            Assert.IsNull(mi.ServiceBundle.Config.LoggingCallback);

            // Assert default cache settings
            Assert.IsNotNull(mi.ServiceBundle.Config.AccessorOptions);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, mi.ServiceBundle.Config.LogLevel);
            Assert.AreEqual(false, mi.ServiceBundle.Config.EnablePiiLogging);
            Assert.AreEqual(false, mi.ServiceBundle.Config.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithCreateUserAssignedId()
        {
            var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(TestConstants.ClientId))
                .BuildConcrete();

            //Assert defaults
            Assert.AreEqual(TestConstants.ClientId, mi.ServiceBundle.Config.ClientId);
            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, mi.ServiceBundle.Config.RedirectUri);
            Assert.AreEqual(Constants.ManagedIdentityDefaultTenant, mi.ServiceBundle.Config.TenantId);

            Assert.IsNotNull(mi.ServiceBundle.Config.ClientName);
            Assert.IsNotNull(mi.ServiceBundle.Config.ClientVersion);

            Assert.IsNotNull(mi.ServiceBundle.Config.ManagedIdentityId);
            Assert.AreEqual(ManagedIdentityIdType.ClientId, mi.ServiceBundle.Config.ManagedIdentityId.IdType);
            Assert.AreEqual(TestConstants.ClientId, mi.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);

            Assert.IsNull(mi.ServiceBundle.Config.HttpClientFactory);
            Assert.IsNull(mi.ServiceBundle.Config.LoggingCallback);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, mi.ServiceBundle.Config.LogLevel);
            Assert.AreEqual(false, mi.ServiceBundle.Config.EnablePiiLogging);
            Assert.AreEqual(false, mi.ServiceBundle.Config.IsDefaultPlatformLoggingEnabled);
        }

        [DataTestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow("resourceId", UserAssignedIdentityId.ResourceId)]
        [DataRow("resourceId/subscription", UserAssignedIdentityId.ResourceId)]
        [DataRow(TestConstants.ObjectId, UserAssignedIdentityId.ObjectId)]
        public void TestConstructor_WithUserAssignedManagedIdentity(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            ManagedIdentityApplication mi = CreateMIABuilder(userAssignedId, userAssignedIdentityId).BuildConcrete();

            Assert.AreEqual(userAssignedId, mi.ServiceBundle.Config.ClientId);
            Assert.IsNotNull(mi.ServiceBundle.Config.ManagedIdentityId);
           
            Assert.AreEqual(userAssignedIdentityId.ToString(), mi.ServiceBundle.Config.ManagedIdentityId.IdType.ToString());
            Assert.AreEqual(userAssignedId, mi.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallback()
        {
            var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithDebugLoggingCallback()
                .BuildConcrete();
            Assert.IsNotNull(mi.ServiceBundle.Config.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = NSubstitute.Substitute.For<IMsalHttpClientFactory>();
            var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpClientFactory(httpClientFactory)
                .BuildConcrete();
            Assert.AreEqual(httpClientFactory, mi.ServiceBundle.Config.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithLogging()
        {
            var mi = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithLogging((_, _, _) => { })
                .BuildConcrete();

            Assert.IsNotNull(mi.ServiceBundle.Config.LoggingCallback);
        }

    }
}
