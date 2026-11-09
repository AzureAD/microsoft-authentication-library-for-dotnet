// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class WindowsBrowserLaunchTests
    {
        private const string AuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=test&state=a%26b%3Dc&code_challenge=synthetic&code_challenge_method=S256";

        [TestMethod]
        [DataRow(AuthorizationUrl)]
        [DataRow("http://localhost:3000/?first=1&second=2")]
        [DataRow("https://example.invalid/?text=%E4%B8%AD%E6%96%87&encoded=%2526&caret=%5E&quote=%22#fragment")]
        public void OpenEdgeBrowserOnWindows_PreservesUrl(string url)
        {
            // Arrange
            var launches = new List<ProcessStartInfo>();

            // Act
            SystemWebViewOptions.OpenEdgeBrowserOnWindows(url, launches.Add);

            // Assert
            Assert.HasCount(1, launches);
            AssertUriLaunch(launches[0], "microsoft-edge:" + url);
        }

        [TestMethod]
        public void OpenEdgeBrowserOnWindows_PropagatesLaunchFailure()
        {
            // Arrange
            var expected = new Win32Exception(1155);
            int launches = 0;

            // Act
            var actual = Assert.ThrowsExactly<Win32Exception>(() =>
                SystemWebViewOptions.OpenEdgeBrowserOnWindows(AuthorizationUrl, _ =>
                {
                    launches++;
                    throw expected;
                }));

            // Assert
            Assert.AreSame(expected, actual);
            Assert.AreEqual(1, launches);
        }

        [TestMethod]
        public void OpenDefaultBrowserOnWindows_SuccessDoesNotLaunchEdge()
        {
            // Arrange
            var launches = new List<ProcessStartInfo>();

            // Act
            SystemWebViewOptions.OpenDefaultBrowserOnWindows(AuthorizationUrl, launches.Add);

            // Assert
            Assert.HasCount(1, launches);
            AssertUriLaunch(launches[0], AuthorizationUrl);
        }

        [TestMethod]
        public void OpenDefaultBrowserOnWindows_FailureLaunchesEdgeWithOriginalUrl()
        {
            // Arrange
            var launches = new List<ProcessStartInfo>();

            // Act
            SystemWebViewOptions.OpenDefaultBrowserOnWindows(AuthorizationUrl, startInfo =>
            {
                launches.Add(startInfo);
                if (launches.Count == 1)
                {
                    throw new Win32Exception(1155);
                }
            });

            // Assert
            Assert.HasCount(2, launches);
            AssertUriLaunch(launches[0], AuthorizationUrl);
            AssertUriLaunch(launches[1], "microsoft-edge:" + AuthorizationUrl);
        }

        [TestMethod]
        public void OpenDefaultBrowserOnWindows_PropagatesFallbackFailure()
        {
            // Arrange
            var launches = new List<ProcessStartInfo>();
            var fallbackFailure = new Win32Exception(2);

            // Act
            var actual = Assert.ThrowsExactly<Win32Exception>(() =>
                SystemWebViewOptions.OpenDefaultBrowserOnWindows(AuthorizationUrl, startInfo =>
                {
                    launches.Add(startInfo);
                    if (launches.Count == 1)
                    {
                        throw new Win32Exception(1155);
                    }

                    throw fallbackFailure;
                }));

            // Assert
            Assert.AreSame(fallbackFailure, actual);
            Assert.HasCount(2, launches);
            AssertUriLaunch(launches[1], "microsoft-edge:" + AuthorizationUrl);
        }

        private static void AssertUriLaunch(ProcessStartInfo startInfo, string expectedUri)
        {
            Assert.AreEqual(expectedUri, startInfo.FileName);
            Assert.IsTrue(startInfo.UseShellExecute);
            Assert.AreEqual(string.Empty, startInfo.Arguments);
            Assert.IsFalse(startInfo.CreateNoWindow);
        }
    }
}
