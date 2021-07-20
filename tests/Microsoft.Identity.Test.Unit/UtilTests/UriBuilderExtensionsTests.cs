// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class UriBuilderExtensionsTests
    {
        [DataTestMethod]
        [DataRow("https://microsoft.com/auth?scope=scope1", "response_mode", "form_post", "https://microsoft.com/auth?scope=scope1&response_mode=form_post")]
        [DataRow("https://microsoft.com/auth?response_mode=query&scope=scope1", "response_mode", "form_post", "https://microsoft.com/auth?response_mode=form_post&scope=scope1")]
        [DataRow("https://microsoft.com/auth?response_mode=form_post&scope=scope1", "response_mode", "form_post", "https://microsoft.com/auth?response_mode=form_post&scope=scope1")]
        public void AppendOrReplaceQueryParameterTest(string originalUriString, string key, string value, string expectedUriString)
        {
            UriBuilder uriBuilder = new UriBuilder(originalUriString);
            uriBuilder.AppendOrReplaceQueryParameter(key, value);

            Assert.AreEqual(expectedUriString, uriBuilder.Uri.ToString());
        }
    }
}
