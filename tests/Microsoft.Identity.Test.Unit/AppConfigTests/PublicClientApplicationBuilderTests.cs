// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client;
#if !NET6_WIN && !NET7_0 && !NET6_0_OR_GREATER
using Microsoft.Identity.Client.Desktop;
#endif
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory(TestCategories.BuilderTests)]
    public class PublicClientApplicationBuilderTests
    {
        [TestMethod]
        public void TestConstructor()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .Build();
            Assert.AreEqual(TestConstants.ClientId, pca.AppConfig.ClientId);
            Assert.IsNotNull(pca.UserTokenCache);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, pca.AppConfig.LogLevel);
            Assert.AreEqual(TestConstants.ClientId, pca.AppConfig.ClientId);
            Assert.IsNotNull(pca.AppConfig.ClientName);
            Assert.IsNotNull(pca.AppConfig.ClientVersion);
            Assert.IsFalse(pca.AppConfig.EnablePiiLogging);
            Assert.IsNull(pca.AppConfig.HttpClientFactory);
            Assert.IsFalse(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
            Assert.IsNull(pca.AppConfig.LoggingCallback);
            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(TestConstants.ClientId), pca.AppConfig.RedirectUri);
            Assert.IsNull(pca.AppConfig.TenantId);
            Assert.IsNull(pca.AppConfig.ParentActivityOrWindowFunc);
        }

        [TestMethod]
        public void TestWithDifferentClientId()
        {
            const string ClientId = "fe81f2b0-4000-433a-915d-5feb0fb2aea5";
            var pca = PublicClientApplicationBuilder.Create(ClientId)
                                                    .Build();
            Assert.AreEqual(ClientId, pca.AppConfig.ClientId);
        }

        [TestMethod]
        public void ClientIdMustBeAGuid()
        {
            var ex = AssertException.Throws<MsalClientException>(
                () => PublicClientApplicationBuilder.Create("http://my.app")
                        .WithAuthority(TestConstants.AadAuthorityWithTestTenantId)
                        .Build());

            Assert.AreEqual(MsalError.ClientIdMustBeAGuid, ex.ErrorCode);

            ex = AssertException.Throws<MsalClientException>(
              () => PublicClientApplicationBuilder.Create("http://my.app")
                      .WithAuthority(TestConstants.B2CAuthority)
                      .Build());

            Assert.AreEqual(MsalError.ClientIdMustBeAGuid, ex.ErrorCode);

            // ADFS does not have this constraint
            PublicClientApplicationBuilder.Create("http://my.app")
                        .WithAuthority(new Uri(TestConstants.ADFSAuthority))
                        .Build();

        }

        [TestMethod]
        public void TestConstructor_ClientIdOverride()
        {
            const string ClientId = "7b94cb0c-3744-4e6e-908b-ae10368b765d";
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithClientId(ClientId)
                                                    .Build();
            Assert.AreEqual(ClientId, pca.AppConfig.ClientId);
        }

        [TestMethod]
        public void TestConstructor_WithClientNameAndVersion()
        {
            const string ClientName = "my client name";
            const string ClientVersion = "1.2.3.4-prerelease";
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithClientName(ClientName)
                                                    .WithClientVersion(ClientVersion)
                                                    .Build();
            Assert.AreEqual(ClientName, pca.AppConfig.ClientName);
            Assert.AreEqual(ClientVersion, pca.AppConfig.ClientVersion);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallback()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithDebugLoggingCallback(LogLevel.Verbose, true, true)
                                                    .Build();
            Assert.IsNotNull(pca.AppConfig.LoggingCallback);
            Assert.AreEqual(LogLevel.Verbose, pca.AppConfig.LogLevel);
            Assert.IsTrue(pca.AppConfig.EnablePiiLogging);
            Assert.IsTrue(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallbackAndAppConfig()
        {
            // Ensure that values in the options are not reset to defaults when not sent into WithLogging
            var options = new PublicClientApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                LogLevel = LogLevel.Error,
                EnablePiiLogging = true,
                IsDefaultPlatformLoggingEnabled = true
            };

            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                .WithLogging((_, _, _) => { }).Build();

            Assert.AreEqual(LogLevel.Error, pca.AppConfig.LogLevel);
            Assert.IsTrue(pca.AppConfig.EnablePiiLogging);
            Assert.IsTrue(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallbackAndAppConfigWithOverride()
        {
            // Ensure that values in the options are reset to new values when sent into WithLogging
            var options = new PublicClientApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                LogLevel = LogLevel.Error,
                EnablePiiLogging = false,
                IsDefaultPlatformLoggingEnabled = true
            };

            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                .WithLogging((_, _, _) => { },
                    LogLevel.Verbose, true, false).Build();

            Assert.AreEqual(LogLevel.Verbose, pca.AppConfig.LogLevel);
            Assert.IsTrue(pca.AppConfig.EnablePiiLogging);
            Assert.IsFalse(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithParentActivityOrWindowFunc()
        {
            IntPtr ownerPtr = new IntPtr(23478);

            var pca = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithParentActivityOrWindow(() => ownerPtr)
                .Build();

            Assert.IsNotNull(pca.AppConfig.ParentActivityOrWindowFunc);
            Assert.AreEqual(ownerPtr, pca.AppConfig.ParentActivityOrWindowFunc());
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void TestConstructor_WithValidInstanceDicoveryMetadata()
        {
            string instanceMetadataJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                   .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                                                   .Build();

            var instanceDiscoveryMetadata = (pca.AppConfig as ApplicationConfiguration).CustomInstanceDiscoveryMetadata;
            Assert.AreEqual(2, instanceDiscoveryMetadata.Metadata.Length);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void TestConstructor_InstanceMetadata_ValidateAuthority_MutuallyExclusive()
        {
            string instanceMetadataJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
            var ex = AssertException.Throws<MsalClientException>(() => PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                  .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                                                  .WithAuthority("https://some.authority/bogus/", true)
                                                  .Build());
            Assert.AreEqual(ex.ErrorCode, MsalError.ValidateAuthorityOrCustomMetadata);
        }

        [TestMethod]
        public void TestConstructor_InstanceMetadataUri_ValidateAuthority_MutuallyExclusive()
        {
            var ex = AssertException.Throws<MsalClientException>(() => PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                  .WithInstanceDiscoveryMetadata(new Uri("https://some_uri.com"))
                                                  .WithAuthority("https://some.authority/bogus/", true)
                                                  .Build());
            Assert.AreEqual(ex.ErrorCode, MsalError.ValidateAuthorityOrCustomMetadata);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void TestConstructor_WithInstanceDiscoveryMetadata_OnlyOneOverload()
        {
            string instanceMetadataJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
            var ex = AssertException.Throws<MsalClientException>(() => PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                  .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                                                  .WithInstanceDiscoveryMetadata(new Uri("https://some_uri.com"))
                                                  .WithAuthority("https://some.authority/bogus/", true)
                                                  .Build());
            Assert.AreEqual(ex.ErrorCode, MsalError.CustomMetadataInstanceOrUri);
        }

        [TestMethod]
        public void TestConstructor_BadInstanceMetadata()
        {
            var ex = AssertException.Throws<MsalClientException>(() => PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                  .WithInstanceDiscoveryMetadata("{bad_json_metadata")
                                                  .Build());

            Assert.AreEqual(ex.ErrorCode, MsalError.InvalidUserInstanceMetadata);
        }

        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = NSubstitute.Substitute.For<IMsalHttpClientFactory>();
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithHttpClientFactory(httpClientFactory)
                                                    .Build();
            Assert.AreEqual(httpClientFactory, pca.AppConfig.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithLogging()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithLogging((_, _, _) => { }, LogLevel.Verbose, true, true)
                                                    .Build();

            Assert.IsNotNull(pca.AppConfig.LoggingCallback);
            Assert.AreEqual(LogLevel.Verbose, pca.AppConfig.LogLevel);
            Assert.IsTrue(pca.AppConfig.EnablePiiLogging);
            Assert.IsTrue(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithRedirectUri()
        {
            const string RedirectUri = "http://some_redirect_uri/";
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithRedirectUri(RedirectUri)
                                                    .Build();

            Assert.AreEqual(RedirectUri, pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithNullRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithRedirectUri(null)
                                                    .Build();

            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(TestConstants.ClientId), pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithEmptyRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithRedirectUri(string.Empty)
                                                    .Build();

            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(TestConstants.ClientId), pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithWhitespaceRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithRedirectUri("    ")
                                                    .Build();

            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(TestConstants.ClientId), pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithConstantsDefaultRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithRedirectUri(Constants.DefaultRedirectUri)
                                                    .Build();

            Assert.AreEqual(Constants.DefaultRedirectUri, pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithTenantId()
        {
            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithTenantId(TestConstants.TenantId)
                                                    .Build();

            Assert.AreEqual(TestConstants.TenantId, pca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestCreateWithOptions()
        {
            var options = new PublicClientApplicationOptions
            {
                Instance = "https://login.microsoftonline.com",
                TenantId = "organizations",
                ClientId = TestConstants.ClientId
            };
            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();
            Assert.AreEqual(TestConstants.AuthorityOrganizationsTenant, pca.Authority);
        }

        [TestMethod]
        public void TestCreateWithOptionsAuthorityAudience()
        {
            var options = new PublicClientApplicationOptions
            {
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
                AadAuthorityAudience = AadAuthorityAudience.AzureAdMultipleOrgs,
                ClientId = TestConstants.ClientId
            };
            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();
            Assert.AreEqual(TestConstants.AuthorityOrganizationsTenant, pca.Authority);
        }

        [TestMethod]
        public void EnsureCreatePublicClientWithAzureAdMyOrgAndNoTenantThrowsException()
        {
            var options = new PublicClientApplicationOptions
            {
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
                AadAuthorityAudience = AadAuthorityAudience.AzureAdMyOrg,
                ClientId = TestConstants.ClientId
            };

            try
            {
                var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                        .Build();
                Assert.Fail("Should have thrown exception here due to missing TenantId");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InvalidOperationException);
            }
        }

        [TestMethod]
        public void EnsureCreatePublicClientWithAzureAdMyOrgAndValidTenantSucceeds()
        {
            const string tenantId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

            var options = new PublicClientApplicationOptions
            {
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
                AadAuthorityAudience = AadAuthorityAudience.AzureAdMyOrg,
                TenantId = tenantId,
                ClientId = TestConstants.ClientId
            };

            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{tenantId}/", pca.Authority);
        }

        [DataTestMethod]
        [DataRow(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdMultipleOrgs, TestConstants.AuthorityOrganizationsTenant, DisplayName = "AzurePublic + AzureAdMultipleOrgs")]
        [DataRow(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, TestConstants.AuthorityCommonTenant, DisplayName = "AzurePublic + AzureAdAndPersonalMicrosoftAccount")]
        [DataRow(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount, "https://login.microsoftonline.com/consumers/", DisplayName = "AzurePublic + PersonalMicrosoftAccount")]
        [DataRow(AzureCloudInstance.AzureChina, AadAuthorityAudience.AzureAdMultipleOrgs, "https://login.chinacloudapi.cn/organizations/", DisplayName = "AzureChina + AzureAdMultipleOrgs")]
        [DataRow(AzureCloudInstance.AzureChina, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, "https://login.chinacloudapi.cn/common/", DisplayName = "AzureChina + AzureAdAndPersonalMicrosoftAccount")]
        [DataRow(AzureCloudInstance.AzureChina, AadAuthorityAudience.PersonalMicrosoftAccount, "https://login.chinacloudapi.cn/consumers/", DisplayName = "AzureChina + PersonalMicrosoftAccount")]
        public void TestAuthorityPermutations(
            AzureCloudInstance cloudInstance,
            AadAuthorityAudience audience,
            string expectedAuthority)
        {
            var options = new PublicClientApplicationOptions
            {
                AzureCloudInstance = cloudInstance,
                AadAuthorityAudience = audience,
                ClientId = TestConstants.ClientId
            };
            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();
            Assert.AreEqual(expectedAuthority, pca.Authority);
        }

        [TestMethod]
        public void TestAuthorities()
        {
            IPublicClientApplication app;

            // No AAD Authority
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .Build();
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);

            // Azure Cloud Instance + AAD Authority Audience
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    AadAuthorityAudience.AzureAdMultipleOrgs)
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/organizations/", app.Authority);

            // Azure Cloud Instance + common
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "common")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/common/", app.Authority);

            // Azure Cloud Instance + consumers
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "consumers")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/consumers/", app.Authority);

            // Azure Cloud Instance + domain
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "contoso.com")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/contoso.com/", app.Authority);

            // Azure Cloud Instance + tenantId(GUID)
            Guid tenantId = Guid.NewGuid();
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    tenantId)
                                                .Build();
            Assert.AreEqual($"https://login.chinacloudapi.cn/{tenantId:D}/", app.Authority);

            // Azure Cloud Instance + tenantId(string)
            tenantId = Guid.NewGuid();
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    tenantId.ToString())
                                                .Build();
            Assert.AreEqual($"https://login.chinacloudapi.cn/{tenantId:D}/", app.Authority);
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)]
        [WorkItem(1320)]
        public void TestAuthorityWithTenant()
        {
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                                                .WithTenantId("contoso.com")
                                                .Build();

            Assert.AreEqual("https://login.microsoftonline.com/contoso.com/", app.Authority);
        }

        [TestMethod]
        public void AuthorityNullArgs()
        {
            AssertException.Throws<ArgumentNullException>(() =>
                PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority((Uri)null));

            AssertException.Throws<ArgumentNullException>(() =>
                PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                  .WithAuthority((string)null));

            AssertException.Throws<ArgumentNullException>(() =>
                PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                  .WithAuthority("  "));

            AssertException.Throws<ArgumentNullException>(() =>
               PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                 .WithAuthority(null, "tid"));

            AssertException.Throws<ArgumentNullException>(() =>
             PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                               .WithAuthority("", "tid"));

            AssertException.Throws<ArgumentNullException>(() =>
            PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority("", Guid.NewGuid()));

            AssertException.Throws<ArgumentNullException>(() =>
                PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                .WithAuthority("https://login.microsoftonline.com/", null));

            AssertException.Throws<ArgumentNullException>(() =>
                PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority("https://login.microsoftonline.com/", " "));

            AssertException.Throws<ArgumentNullException>(() =>
                PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithAuthority(AzureCloudInstance.AzureChina, ""));
        }

        [TestMethod]
        public void TestAadAuthorityWithInvalidSegmentCount()
        {
            try
            {
                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority("https://login.microsoftonline.fr")
                                                        .Build();
                Assert.Fail("Should not reach here, exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException);
                Assert.IsTrue(ex.Message.Contains(MsalErrorMessage.AuthorityUriInvalidPath));
            }
        }

        [TestMethod]
        public void WithClientCapabilities()
        {
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientCapabilities(new[] { "cp1", "cp2" })
                .Build();

            CollectionAssert.AreEquivalent(new[] { "cp1", "cp2" }, app.AppConfig.ClientCapabilities.ToList());
        }

        [TestMethod]
        public void WithClientCapabilitiesViaOptions()
        {
            var options = new PublicClientApplicationOptions
            {
                Instance = "https://login.microsoftonline.com",
                TenantId = "organizations",
                ClientId = TestConstants.ClientId,
                ClientCapabilities = new[] { "cp1", "cp2" }
            };

            var app = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                .Build();

            CollectionAssert.AreEquivalent(new string[] { "cp1", "cp2" }, app.AppConfig.ClientCapabilities.ToList());
        }

        [TestMethod]
        // bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2543
        public void AuthorityWithTenant()
        {
            var options = new PublicClientApplicationOptions();
            options.ClientId = TestConstants.ClientId;

            var app1 = PublicClientApplicationBuilder
                .CreateWithApplicationOptions(options)
                .WithTenantId(TestConstants.TenantId)
                .WithAuthority("https://login.microsoftonline.com/common")
               .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{TestConstants.TenantId}/", app1.Authority);

            var app2 = PublicClientApplicationBuilder
               .CreateWithApplicationOptions(options)
               .WithTenantId(TestConstants.TenantId)
              .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{TestConstants.TenantId}/", app2.Authority);

            PublicClientApplicationBuilder
             .CreateWithApplicationOptions(options)
             .WithTenantId(TestConstants.TenantId)
             .WithAuthority($"https://login.microsoftonline.com/{TestConstants.TenantId2}")
            .Build();

            var options2 = new PublicClientApplicationOptions();
            options2.ClientId = TestConstants.ClientId;
            options2.TenantId = TestConstants.TenantId;

            var app4 = PublicClientApplicationBuilder
                .CreateWithApplicationOptions(options2)
                .WithAuthority("https://login.microsoftonline.com/common")
               .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{TestConstants.TenantId}/", app4.Authority);

            var app5 = PublicClientApplicationBuilder
                .CreateWithApplicationOptions(options)
                .WithAuthority($"https://login.microsoftonline.com/{TestConstants.TenantId}")
                .WithTenantId($"{TestConstants.TenantId}")
               .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{TestConstants.TenantId}/", app5.Authority);

            var app6 = PublicClientApplicationBuilder
             .CreateWithApplicationOptions(options2)
             .WithAuthority($"https://login.microsoftonline.com/{TestConstants.TenantId}")
            .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{TestConstants.TenantId}/", app6.Authority);

            var app7 = PublicClientApplicationBuilder
                .CreateWithApplicationOptions(options2)
                .WithAuthority($"https://login.microsoftonline.com/{TestConstants.TenantId}")
                .WithTenantId($"{TestConstants.TenantId}")
                .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{TestConstants.TenantId}/", app6.Authority);
        }

        [TestMethod]
        public void CacheSynchronization_Default_IsTrue()
        {
            var pcaOptions = new PublicClientApplicationOptions()
            {
                ClientId = TestConstants.ClientId
            };
            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(pcaOptions).Build();
            Assert.IsTrue((pca.AppConfig as ApplicationConfiguration).CacheSynchronizationEnabled);

            pca = PublicClientApplicationBuilder.Create(Guid.NewGuid().ToString()).Build();
            Assert.IsTrue((pca.AppConfig as ApplicationConfiguration).CacheSynchronizationEnabled);
        }

#if NET6_WIN
        [TestMethod]
        public void IsBrokerAvailable_net6()
        {
            var appBuilder = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTenant);

            Assert.AreEqual(DesktopOsHelper.IsWin10OrServerEquivalent(), appBuilder.IsBrokerAvailable());
        }
#endif

        [TestMethod]
        public void IsBrokerAvailable_NoAuthorityInBuilder()
        {
            var builder1 = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId);

#if NETFRAMEWORK || NET_CORE
            Assert.IsFalse(builder1.IsBrokerAvailable());
#else
            Assert.IsTrue(builder1.IsBrokerAvailable());
#endif
        }
    }
}
