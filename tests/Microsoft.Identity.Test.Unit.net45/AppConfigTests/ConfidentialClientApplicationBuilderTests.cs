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
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory("BuilderTests")]
    public class ConfidentialClientApplicationBuilderTests
    {
        [TestMethod]
        public void TestConstructor()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId).Build();
            Assert.AreEqual(MsalTestConstants.ClientId, cca.AppConfig.ClientId);
            Assert.IsNotNull(cca.UserTokenCache);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, cca.AppConfig.LogLevel);
            Assert.AreEqual(MsalTestConstants.ClientId, cca.AppConfig.ClientId);
            Assert.IsNull(cca.AppConfig.Component);
            Assert.AreEqual(false, cca.AppConfig.EnablePiiLogging);
            Assert.IsNull(cca.AppConfig.HttpClientFactory);
            Assert.AreEqual(false, cca.AppConfig.IsDefaultPlatformLoggingEnabled);
            Assert.IsNull(cca.AppConfig.LoggingCallback);
            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
            Assert.IsNull(cca.AppConfig.TelemetryCallback);
            Assert.AreEqual(null, cca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestWithDifferentClientId()
        {
            const string ClientId = "9340c42a-f5de-4a80-aea0-874adc2ca325";
            var cca = ConfidentialClientApplicationBuilder.Create(ClientId).Build();
            Assert.AreEqual(ClientId, cca.AppConfig.ClientId);
        }

        [TestMethod]
        public void TestConstructor_ClientIdOverride()
        {
            const string ClientId = "73cc145e-798f-430c-8d6d-618f1a5802e9";
            var cca = ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithClientId(ClientId).Build();
            Assert.AreEqual(ClientId, cca.AppConfig.ClientId);
        }

        [TestMethod]
        public void TestConstructor_WithComponent()
        {
            const string Component = "my component name";
            var cca =
                ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithComponent(Component).Build();
            Assert.AreEqual(Component, cca.AppConfig.Component);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallback()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithDebugLoggingCallback().Build();
            Assert.IsNotNull(cca.AppConfig.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = new MyHttpClientFactory();  
            var cca = ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithHttpClientFactory(httpClientFactory).Build();
            Assert.AreEqual(httpClientFactory, cca.AppConfig.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithLogging()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithLogging(((level, message, pii) => { })).Build();

            Assert.IsNotNull(cca.AppConfig.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithRedirectUri()
        {
            const string RedirectUri = "http://some_redirect_uri/";
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithRedirectUri(RedirectUri).Build();

            Assert.AreEqual(RedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithNullRedirectUri()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithRedirectUri(null).Build();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithEmptyRedirectUri()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithRedirectUri(string.Empty).Build();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithWhitespaceRedirectUri()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithRedirectUri("      ").Build();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithInvalidRedirectUri()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                    .WithRedirectUri("this is not a valid uri")
                                                    .Build());
        }

        [TestMethod]
        public void TestConstructor_WithTenantId()
        {
            const string TenantId = "a_tenant id";
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithTenantId(TenantId).Build();

            Assert.AreEqual(TenantId, cca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestConstructor_WithTelemetry()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithTelemetry((events => { })).Build();

            Assert.IsNotNull(cca.AppConfig.TelemetryCallback);
        }

        [TestMethod]
        public void TestConstructor_WithClientSecret()
        {
            const string ClientSecret = "secret value here";
            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithClientSecret(ClientSecret).Build();

            Assert.IsNotNull(cca.AppConfig.ClientSecret);
            Assert.AreEqual(ClientSecret, cca.AppConfig.ClientSecret);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        public void TestConstructor_WithCertificate_X509Certificate2()
        {
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");

            var cca = ConfidentialClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithCertificate(cert).Build();

            Assert.IsNotNull(cca.AppConfig.ClientCredentialCertificate);
        }
    }
}
