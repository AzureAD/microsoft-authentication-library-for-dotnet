// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET_CORE 
using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.Platforms.Shared.NetStdCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class NetCoreWebUIFactoryTests
    {
        private readonly NetCoreWebUIFactory _webUIFactory = new NetCoreWebUIFactory();
        private readonly CoreUIParent _parent = new CoreUIParent();
        private readonly RequestContext _requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

        [TestMethod]
        public void NetCoreFactory_DefaultEmbedded()
        {
            // Arrange

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(
                _parent, 
                WebViewPreference.NotSpecified, 
                _requestContext);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
        }

        [TestMethod]
        public void NetCoreFactory_Embedded()
        {
            // Arrange

            // Act
           var ex = AssertException.Throws<MsalClientException>(
               ()=> _webUIFactory.CreateAuthenticationDialog(
                    _parent, 
                    WebViewPreference.Embedded, 
                    _requestContext));

            // Assert
            Assert.AreEqual(MsalError.WebViewUnavailable, ex.ErrorCode);
        }

        [TestMethod]
        public void NetCoreFactory_System()
        {
            // Arrange

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(
                _parent, 
                WebViewPreference.System, 
                _requestContext);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
            Assert.IsTrue(_webUIFactory.IsSystemWebViewAvailable);
        }
    }
}
#endif
