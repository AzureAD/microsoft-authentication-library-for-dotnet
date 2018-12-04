// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class RedirectUriHelperTests
    {
        [TestMethod]
        public void ValidateRedirectUri_Throws()
        {
          
            Assert.ThrowsException<MsalClientException>(
                () => RedirectUriHelper.Validate(null));

            Assert.ThrowsException<ArgumentException>(
               () => RedirectUriHelper.Validate(new Uri("https://redirectUri/uri#fragment")),
               "Validatation should fail if uri has a fragment, i.e. #foo");
        }

        [TestMethod]
        public void ValidateRedirectUri_DoesNotThrow()
        {
            // Arrange
            Uri inputUri = new Uri("http://redirectUri");

            // Act
            RedirectUriHelper.Validate(inputUri);

            // Assert
            // no exception is thrown
        }

        [TestMethod]
        public void ValidateRedirectUri_NoOAuth2DefaultWhenUsingSystemBrowser()
        {
            Assert.ThrowsException<MsalClientException>(() =>
                RedirectUriHelper.Validate(new Uri(Constants.DefaultRedirectUri), true));

              RedirectUriHelper.Validate(new Uri(Constants.DefaultRedirectUri), false);
        }
    }
}