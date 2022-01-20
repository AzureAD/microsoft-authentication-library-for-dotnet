// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class EmbeddedUiTests
    {
        [TestMethod]
        public void IsAllowedIeOrEdgeAuthorizationRedirect()
        {
            Assert.IsTrue(
                EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(
                    new System.Uri("https://login.microsoft.com/v2.0/authorize/some_page")));

            Assert.IsTrue(
                EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(
                    new System.Uri("javascript://bing.com/script.js")));

            Assert.IsTrue(
                EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(
                    new System.Uri("res://404.html")));

            Assert.IsTrue(
                EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(
                    new System.Uri("about:blank")));

            Assert.IsFalse(
               EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(
                   new System.Uri("about:blank.com")));

            Assert.IsFalse(
               EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(
                   new System.Uri("http://microsoft.com")));

        }
    }
}
