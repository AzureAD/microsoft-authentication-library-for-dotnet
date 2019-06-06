// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if DESKTOP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.net45;
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
        public void Net45Factory_DefaultEmbedded()
        {
            // Arrange
            _parent.UseEmbeddedWebview = true;

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(_parent, _requestContext);

            // Assert
            Assert.IsTrue(webUi is InteractiveWebUI);
        }

        [TestMethod]
        public void Net45Factory_SilentWebUi()
        {
            // Arrange
            _parent.UseHiddenBrowser = true;

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(_parent, _requestContext);

            // Assert
            Assert.IsTrue(webUi is SilentWebUI);
        }

        [TestMethod]
        public void Net45Factory_SystemWebUi()
        {
            // Arrange
            _parent.UseEmbeddedWebview = false;

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(_parent, _requestContext);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
        }
    }
}
#endif
