// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Test.Common.Core.Helpers;
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
            Assert.AreEqual(MsalTestConstants.ClientId, pca.ClientId);
            Assert.IsNotNull(pca.UserTokenCache);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, pca.AppConfig.LogLevel);
            Assert.IsNull(pca.AppConfig.ClientCredential);
            Assert.AreEqual(MsalTestConstants.ClientId, pca.AppConfig.ClientId);
            Assert.IsNull(pca.AppConfig.Component);
            Assert.IsFalse(pca.AppConfig.EnablePiiLogging);
            Assert.IsNull(pca.AppConfig.HttpClientFactory);
            Assert.IsFalse(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
            Assert.IsNull(pca.AppConfig.LoggingCallback);
            Assert.AreEqual(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(MsalTestConstants.ClientId), pca.AppConfig.RedirectUri);
            Assert.IsNull(pca.AppConfig.TelemetryCallback);
            Assert.IsNull(pca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestWithDifferentClientId()
        {
            const string ClientId = "fe81f2b0-4000-433a-915d-5feb0fb2aea5";
            var pca = PublicClientApplicationBuilder.Create(ClientId)
                                                    .Build();
            Assert.AreEqual(ClientId, pca.ClientId);
        }

        [TestMethod]
        public void TestConstructor_ClientIdOverride()
        {
            const string ClientId = "7b94cb0c-3744-4e6e-908b-ae10368b765d";
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithClientId(ClientId)
                                                    .Build();
            Assert.AreEqual(ClientId, pca.ClientId);
        }

        [TestMethod]
        public void TestConstructor_WithComponent()
        {
            const string Component = "my component name";
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithComponent(Component)
                                                    .Build();
            Assert.AreEqual(Component, pca.AppConfig.Component);
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
            var httpClientFactory = new MyHttpClientFactory();
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
        public void TestConstructor_WithTelemetry()
        {
            void Callback(List<Dictionary<string, string>> events)
            {
            }

            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithTelemetry(Callback)
                                                    .Build();

            Assert.IsNotNull(pca.AppConfig.TelemetryCallback);
            Assert.AreEqual((TelemetryCallback)Callback, pca.AppConfig.TelemetryCallback);
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
        public void TestAuthorities()
        {
            IPublicClientApplication app;

            // No AAD Authority
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .Build();
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);

            // Azure Cloud Instance + AAD Authority Audience
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAadAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    AadAuthorityAudience.AzureAdMultipleOrgs)
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/organizations/", app.Authority);

            // Azure Cloud Instance + common
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAadAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "common")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/common/", app.Authority);

            // Azure Cloud Instance + consumers
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAadAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "consumers")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/consumers/", app.Authority);

            // Azure Cloud Instance + domain
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAadAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    "contoso.com")
                                                .Build();
            Assert.AreEqual("https://login.chinacloudapi.cn/contoso.com/", app.Authority);

            // Azure Cloud Instance + tenantId(GUID)
            Guid tenantId = Guid.NewGuid();
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAadAuthority(
                                                    AzureCloudInstance.AzureChina,
                                                    tenantId)
                                                .Build();
            Assert.AreEqual($"https://login.chinacloudapi.cn/{tenantId:D}/", app.Authority);

            // Azure Cloud Instance + tenantId(string)
            tenantId = Guid.NewGuid();
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                .WithAadAuthority(
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
                                                    .WithAadAuthority(AadAuthorityAudience.AzureAdMyOrg)
                                                    .WithTenantId("contoso.com")
                                                    .Build();
                Assert.Fail("Should not reach here, exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InvalidOperationException);
                Assert.AreEqual(CoreErrorMessages.AzureAdMyOrgRequiresSpecifyingATenant, ex.Message);
            }
        }

        [TestMethod]
        public void TestAadAuthorityWithInvalidSegmentCount()
        {
            try
            {
                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAadAuthority("https://login.microsoftonline.fr")
                                                        .Build();
                Assert.Fail("Should not reach here, exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InvalidOperationException);
                Assert.AreEqual(CoreErrorMessages.AuthorityDoesNotHaveTwoSegments, ex.Message);
            }
        }
    }
}