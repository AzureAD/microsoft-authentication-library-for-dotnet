// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if DESKTOP || NET_CORE
using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Desktop;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class MsalDesktopWebUiTests
    {
        private readonly CoreUIParent _parent = new CoreUIParent();
        private readonly RequestContext _requestContext =
            new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

        [TestMethod]
        public void IsSystemWebUiAvailable()
        {
            var webUIFactory = new MsalDesktopWebUiFactory();

            Assert.IsTrue(webUIFactory.IsSystemWebViewAvailable);
        }

        [TestMethod]
        public void DefaultEmbedded_WebView2Available()
        {
            // Arrange
            var webUIFactory = new MsalDesktopWebUiFactory(
                fallbackToLegacyWebBrowser: false, 
                isWebView2AvailableForTest: () => true);


            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContext);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);
        }
#if DESKTOP
        [TestMethod]
        public void DefaultEmbedded_Legacy()
        {
            // Arrange
            var webUIFactory = new MsalDesktopWebUiFactory(
                fallbackToLegacyWebBrowser: true,
                isWebView2AvailableForTest: () => false);


            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContext);

            // Assert
            Assert.IsTrue(webUi is Client.Platforms.Features.WinFormsLegacyWebUi.InteractiveWebUI);
        }
#endif

        [TestMethod]
        public void DefaultEmbedded_WebView2NotAvailable()
        {
            // Arrange
            var webUIFactory = new MsalDesktopWebUiFactory(
               fallbackToLegacyWebBrowser: false,
               isWebView2AvailableForTest: () => false);


            // Act
            var ex = AssertException.Throws<MsalClientException>(() =>
                webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContext));

            // Assert
            Assert.AreEqual(MsalError.WebView2NotInstalled, ex.ErrorCode);
        }

        [TestMethod]
        public void Embedded()
        {
            // Arrange
            var webUIFactory = new MsalDesktopWebUiFactory(
               fallbackToLegacyWebBrowser: false,
               isWebView2AvailableForTest: () => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.Embedded, _requestContext);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);
        }

        [TestMethod]
        public void NetCoreFactory_System()
        {
            // Arrange
            var webUIFactory = new MsalDesktopWebUiFactory(
              fallbackToLegacyWebBrowser: false,
              isWebView2AvailableForTest: () => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                _parent, 
                WebViewPreference.System, 
                _requestContext);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
        }
    }
}

#endif
