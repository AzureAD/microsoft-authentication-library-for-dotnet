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

using System.Net.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
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
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).Build();
            Assert.AreEqual(MsalTestConstants.ClientId, pca.ClientId);
            Assert.IsNotNull(pca.UserTokenCache);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Warning, pca.AppConfig.LogLevel);
            Assert.IsNull(pca.AppConfig.ClientCredential);
            Assert.AreEqual(MsalTestConstants.ClientId, pca.AppConfig.ClientId);
            Assert.IsNull(pca.AppConfig.Component);
            Assert.AreEqual(false, pca.AppConfig.EnablePiiLogging);
            Assert.IsNull(pca.AppConfig.HttpClientFactory);
            Assert.AreEqual(false, pca.AppConfig.IsDefaultPlatformLoggingEnabled);
            Assert.IsNull(pca.AppConfig.LoggingCallback);
            Assert.AreEqual(Constants.DefaultRedirectUri, pca.AppConfig.RedirectUri);
            Assert.IsNull(pca.AppConfig.TelemetryCallback);
            Assert.AreEqual(null, pca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestWithDifferentClientId()
        {
            var pca = PublicClientApplicationBuilder.Create("this is a test client id").Build();
            Assert.AreEqual("this is a test client id", pca.ClientId);
        }

        [TestMethod]
        public void TestConstructor_ClientIdOverride()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithClientId("some other client id").Build();
            Assert.AreEqual("some other client id", pca.ClientId);
        }

        [TestMethod]
        public void TestConstructor_WithComponent()
        {
            var pca =
                PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithComponent("my component name").Build();
            Assert.AreEqual("my component name", pca.AppConfig.Component);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallback()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithDebugLoggingCallback().Build();
            Assert.IsNotNull(pca.AppConfig.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithDefaultPlatformLoggingEnabledTrue()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithDefaultPlatformLoggingEnabled(true).Build();
            Assert.IsTrue(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithDefaultPlatformLoggingEnabledFalse()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithDefaultPlatformLoggingEnabled(false).Build();
            Assert.IsFalse(pca.AppConfig.IsDefaultPlatformLoggingEnabled);
        }

        [TestMethod]
        public void TestConstructor_WithWithEnablePiiLoggingTrue()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithEnablePiiLogging(true).Build();
            Assert.IsTrue(pca.AppConfig.EnablePiiLogging);
        }

        [TestMethod]
        public void TestConstructor_WithWithEnablePiiLoggingFalse()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithEnablePiiLogging(false).Build();
            Assert.IsFalse(pca.AppConfig.EnablePiiLogging);
        }

        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = new MyHttpClientFactory();  
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithHttpClientFactory(httpClientFactory).Build();
            Assert.AreEqual(httpClientFactory, pca.AppConfig.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithLoggingCallback()
        {
            var pca = PublicClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithLoggingCallback(((level, message, pii) => { })).Build();

            Assert.IsNotNull(pca.AppConfig.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithLoggingLevel()
        {
            var pca = PublicClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithLoggingLevel(LogLevel.Verbose).Build();

            Assert.AreEqual(LogLevel.Verbose, pca.AppConfig.LogLevel);
        }

        [TestMethod]
        public void TestConstructor_WithRedirectUri()
        {
            var pca = PublicClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithRedirectUri("http://some_redirect_uri/").Build();

            Assert.AreEqual("http://some_redirect_uri/", pca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithTenantId()
        {
            var pca = PublicClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithTenantId("a_tenant id").Build();

            Assert.AreEqual("a_tenant id", pca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestConstructor_WithTelemetryCallback()
        {
            var pca = PublicClientApplicationBuilder
                      .Create(MsalTestConstants.ClientId).WithTelemetryCallback((events => { })).Build();

            Assert.IsNotNull(pca.AppConfig.TelemetryCallback);
        }
    }
}