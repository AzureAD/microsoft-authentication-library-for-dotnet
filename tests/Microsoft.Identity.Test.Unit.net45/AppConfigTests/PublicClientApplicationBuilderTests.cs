// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory("BuilderTests")]
    public class PublicClientApplicationBuilderTests
    {
        [TestMethod]
        public void TestConstructor()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .Build();
            Assert.AreEqual(MsalTestConstants.ClientId, pca.AppConfig.ClientId);
            Assert.IsNotNull(pca.UserTokenCache);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, pca.AppConfig.LogLevel);
            Assert.AreEqual(MsalTestConstants.ClientId, pca.AppConfig.ClientId);
            Assert.IsNotNull(pca.AppConfig.ClientName);
            Assert.IsNotNull(pca.AppConfig.ClientVersion);
            Assert.IsFalse(pca.AppConfig.EnablePiiLogging);
            Assert.IsNull(pca.AppConfig.HttpClientFactory);
            Assert.IsFalse(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
            Assert.IsNull(pca.AppConfig.LoggingCallback);
            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(MsalTestConstants.ClientId), pca.AppConfig.RedirectUri);
            Assert.IsNull(pca.AppConfig.TenantId);
            Assert.IsNull(pca.AppConfig.TelemetryConfig);
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
        public void TestConstructor_ClientIdOverride()
        {
            const string ClientId = "7b94cb0c-3744-4e6e-908b-ae10368b765d";
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithClientId(ClientId)
                                                    .Build();
            Assert.AreEqual(ClientId, pca.AppConfig.ClientId);
        }

        [TestMethod]
        public void TestConstructor_WithClientNameAndVersion()
        {
            const string ClientName = "my client name";
            const string ClientVersion = "1.2.3.4-prerelease";
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithClientName(ClientName)
                                                    .WithClientVersion(ClientVersion)
                                                    .Build();
            Assert.AreEqual(ClientName, pca.AppConfig.ClientName);
            Assert.AreEqual(ClientVersion, pca.AppConfig.ClientVersion);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallback()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
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
                ClientId = MsalTestConstants.ClientId,
                LogLevel = LogLevel.Error,
                EnablePiiLogging = true,
                IsDefaultPlatformLoggingEnabled = true
            };

            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                .WithLogging((level, message, pii) => { }).Build();

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
                ClientId = MsalTestConstants.ClientId,
                LogLevel = LogLevel.Error,
                EnablePiiLogging = false,
                IsDefaultPlatformLoggingEnabled = true
            };

            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                .WithLogging((level, message, pii) => { },
                    LogLevel.Verbose, true, false).Build();

            Assert.AreEqual(LogLevel.Verbose, pca.AppConfig.LogLevel);
            Assert.IsTrue(pca.AppConfig.EnablePiiLogging);
            Assert.IsFalse(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }


        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = NSubstitute.Substitute.For<IMsalHttpClientFactory>();
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithHttpClientFactory(httpClientFactory)
                                                    .Build();
            Assert.AreEqual(httpClientFactory, pca.AppConfig.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithLogging()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithLogging((level, message, pii) => { }, LogLevel.Verbose, true, true)
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
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithRedirectUri(RedirectUri)
                                                    .Build();

            Assert.AreEqual(RedirectUri, pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithNullRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithRedirectUri(null)
                                                    .Build();

            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(MsalTestConstants.ClientId), pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithEmptyRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithRedirectUri(string.Empty)
                                                    .Build();

            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(MsalTestConstants.ClientId), pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithWhitespaceRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithRedirectUri("    ")
                                                    .Build();

            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(MsalTestConstants.ClientId), pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithConstantsDefaultRedirectUri()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithRedirectUri(Constants.DefaultRedirectUri)
                                                    .Build();

            Assert.AreEqual(Constants.DefaultRedirectUri, pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithTenantId()
        {
            const string TenantId = "a_tenant id";
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithTenantId(TenantId)
                                                    .Build();

            Assert.AreEqual(TenantId, pca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestCreateWithOptions()
        {
            var options = new PublicClientApplicationOptions
            {
                Instance = "https://login.microsoftonline.com",
                TenantId = "organizations",
                ClientId = MsalTestConstants.ClientId
            };
            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();
            Assert.AreEqual(MsalTestConstants.AuthorityOrganizationsTenant, pca.Authority);
        }

        [TestMethod]
        public void TestCreateWithOptionsAuthorityAudience()
        {
            var options = new PublicClientApplicationOptions
            {
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
                AadAuthorityAudience = AadAuthorityAudience.AzureAdMultipleOrgs,
                ClientId = MsalTestConstants.ClientId
            };
            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();
            Assert.AreEqual(MsalTestConstants.AuthorityOrganizationsTenant, pca.Authority);
        }

        [TestMethod]
        public void EnsureCreatePublicClientWithAzureAdMyOrgAndNoTenantThrowsException()
        {
            var options = new PublicClientApplicationOptions
            {
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
                AadAuthorityAudience = AadAuthorityAudience.AzureAdMyOrg,
                ClientId = MsalTestConstants.ClientId
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
                ClientId = MsalTestConstants.ClientId
            };

            var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                    .Build();

            Assert.AreEqual($"https://login.microsoftonline.com/{tenantId}/", pca.Authority);
        }

        [DataTestMethod]
        [DataRow(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdMultipleOrgs, MsalTestConstants.AuthorityOrganizationsTenant, DisplayName = "AzurePublic + AzureAdMultipleOrgs")]
        [DataRow(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, MsalTestConstants.AuthorityCommonTenant, DisplayName = "AzurePublic + AzureAdAndPersonalMicrosoftAccount")]
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
                ClientId = MsalTestConstants.ClientId
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
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .Build();
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);

            // Azure Cloud Instance + AAD Authority Audience
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    AadAuthorityAudience.AzureAdMultipleOrgs)
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/organizations/", app.Authority);

            // Azure Cloud Instance + common
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "common")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/common/", app.Authority);

            // Azure Cloud Instance + consumers
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "consumers")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/consumers/", app.Authority);

            // Azure Cloud Instance + domain
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "contoso.com")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/contoso.com/", app.Authority);

            // Azure Cloud Instance + tenantId(GUID)
            Guid tenantId = Guid.NewGuid();
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    tenantId)
                                                .Build();
            Assert.AreEqual($"https://login.chinacloudapi.cn/{tenantId:D}/", app.Authority);

            // Azure Cloud Instance + tenantId(string)
            tenantId = Guid.NewGuid();
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    tenantId.ToString())
                                                .Build();
            Assert.AreEqual($"https://login.chinacloudapi.cn/{tenantId:D}/", app.Authority);
        }

        [TestMethod]
        public void TestAuthorityInvalidTenant()
        {
            try
            {
                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                                                    .WithTenantId("contoso.com")
                                                    .Build();
                Assert.Fail("Should not reach here, exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InvalidOperationException);
                Assert.AreEqual(MsalErrorMessage.AzureAdMyOrgRequiresSpecifyingATenant, ex.Message);
            }
        }

        [TestMethod]
        public void TestAadAuthorityWithInvalidSegmentCount()
        {
            try
            {
                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority("https://login.microsoftonline.fr")
                                                        .Build();
                Assert.Fail("Should not reach here, exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InvalidOperationException);
                Assert.AreEqual(MsalErrorMessage.AuthorityDoesNotHaveTwoSegments, ex.Message);
            }
        }

        [TestMethod]
        public void MatsAndTelemetryCallbackCannotBothBeConfiguredAtTheSameTime()
        {
            try
            {
                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                    .WithTelemetry((List<Dictionary<string, string>> events) => {})
                    .WithTelemetry(new TelemetryConfig())
                    .Build();
                Assert.Fail("Should not reach here, exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InvalidOperationException);
                Assert.AreEqual(MsalErrorMessage.MatsAndTelemetryCallbackCannotBeConfiguredSimultaneously, ex.Message);
            }
        }

        [TestMethod]
        public void MatsCanBeProperlyConfigured()
        {
            var telemetryConfig = new TelemetryConfig
            {
                SessionId = "some session id"
            };

            var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                .WithTelemetry(telemetryConfig)
                .Build();

            Assert.IsNotNull(app.AppConfig.TelemetryConfig);
            Assert.AreEqual<string>(telemetryConfig.SessionId, app.AppConfig.TelemetryConfig.SessionId);
        }
    }
}
