// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NETFRAMEWORK || NET6_WIN

using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class WebView2WebUiFactoryTests
    {
        private readonly CoreUIParent _parent = new CoreUIParent();
        private readonly RequestContext _requestContextAad =
            new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

        private readonly RequestContext _requestContextB2C =
            new RequestContext(TestCommon.CreateDefaultB2CServiceBundle(), Guid.NewGuid());

        private readonly RequestContext _requestContextAdfs =
          new RequestContext(TestCommon.CreateDefaultAdfsServiceBundle(), Guid.NewGuid());

        [TestMethod]
#if ONEBRANCH_BUILD
        [Ignore]
#endif
        public void IsSystemWebUiAvailable()
        {
            var webUIFactory = new WebView2WebUiFactory();

            Assert.IsTrue(webUIFactory.IsSystemWebViewAvailable);
        }

        [DataRow()]
        public void WebViewTypeNotConfigured_AAD_WebView1()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContextAad);

            // Assert
            Assert.IsTrue(webUi is InteractiveWebUI);

        }

        [TestMethod]
        public void WebViewTypeNotConfigured_ADFS_WebView2()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContextAdfs);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);

        }

        [TestMethod]
        public void WebViewTypeNotConfigured_B2C_WebView2()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContextB2C);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);
        }

        [TestMethod]
        public void DefaultEmbedded_WebView2NotAvailable()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(
                isWebView2AvailableForTest: () => false);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContextAad);

            // Assert
            Assert.IsTrue(webUi is InteractiveWebUI);
        }

        [TestMethod]
        public void WebViewTypeEmbedded_AAD_WebView1()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.Embedded, _requestContextAad);

            // Assert
            Assert.IsTrue(webUi is InteractiveWebUI);
        }

        [TestMethod]
        public void WebViewTypeEmbedded_ADFS_WebView2()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.Embedded, _requestContextAdfs);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);
        }

        [TestMethod]
        public void WebViewTypeEmbedded_B2C_WebView2()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.Embedded, _requestContextB2C);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);
        }

        [TestMethod]
#if ONEBRANCH_BUILD
        [Ignore]
#endif
        public void NetCoreFactory_System()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory();

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.System, _requestContextAad);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
        }
    }
}
#endif
