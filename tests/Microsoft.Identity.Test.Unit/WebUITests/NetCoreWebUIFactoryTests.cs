// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET_CORE
using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.netstandardcore.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
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
        public void Net45Factory_DefaultEmbedded()
        {
            // Arrange

            // Act
            var webUi = _webUIFactory.CreateAuthenticationDialog(_parent, _requestContext);

            // Assert
            Assert.IsTrue(webUi is DefaultOsBrowserWebUi);
        }


    }
}
#endif
