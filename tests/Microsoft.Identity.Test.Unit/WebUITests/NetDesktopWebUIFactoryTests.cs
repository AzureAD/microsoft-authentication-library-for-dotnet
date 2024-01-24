// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NETFRAMEWORK
using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi;
using Microsoft.Identity.Client.Platforms.netdesktop;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class NetDesktopWebUIFactoryTests
    {
        private readonly NetDesktopWebUIFactory _webUIFactory = new NetDesktopWebUIFactory();
        private readonly CoreUIParent _parent = new CoreUIParent();
        private readonly RequestContext _requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

        [TestMethod]
        public void NetFxFactory_DefaultEmbedded()
        {
            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(
                _parent, 
                Client.ApiConfig.Parameters.WebViewPreference.Embedded,
                _requestContext);

            // Assert
            Assert.IsTrue(webUi is InteractiveWebUI);
        }

        [TestMethod]
        public void NetFxFactory_SilentWebUi()
        {
            // Arrange
            _parent.UseHiddenBrowser = true;

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(
                _parent, 
                Client.ApiConfig.Parameters.WebViewPreference.NotSpecified,
                _requestContext);

            // Assert
            Assert.IsTrue(webUi is SilentWebUI);
        }

        [TestMethod]
#if ONEBRANCH_BUILD
        [Ignore]
#endif
        public void NetFxFactory_SystemWebUi()
        {

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(
                           _parent,
                           Client.ApiConfig.Parameters.WebViewPreference.System,
                           _requestContext);
            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
            Assert.IsTrue(_webUIFactory.IsSystemWebViewAvailable);
        }
    }
}
#endif
