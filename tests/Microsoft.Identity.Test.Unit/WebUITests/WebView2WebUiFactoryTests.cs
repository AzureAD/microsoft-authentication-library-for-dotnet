#if DESKTOP || NET_CORE || NET5_WIN

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
        private readonly RequestContext _requestContext =
            new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

        [TestMethod]
        public void IsSystemWebUiAvailable()
        {
            var webUIFactory = new WebView2WebUiFactory();

            Assert.IsTrue(webUIFactory.IsSystemWebViewAvailable);
        }

        [TestMethod]
        public void DefaultEmbedded_WebView2Available()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);


            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(
                    _parent,
                    WebViewPreference.NotSpecified,
                    _requestContext);

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
                    _requestContext);

            // Assert
            Assert.IsTrue(webUi is InteractiveWebUI);
        }

        [TestMethod]
        public void Embedded()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory(() => true);

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.Embedded, _requestContext);

            // Assert
            Assert.IsTrue(webUi is WebView2WebUi);
        }

        [TestMethod]
        public void NetCoreFactory_System()
        {
            // Arrange
            var webUIFactory = new WebView2WebUiFactory();

            // Act
            var webUi = webUIFactory.CreateAuthenticationDialog(_parent, WebViewPreference.System, _requestContext);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
        }
    }
}
#endif
