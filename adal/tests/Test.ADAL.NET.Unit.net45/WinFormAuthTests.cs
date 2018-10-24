//------------------------------------------------------------------------------
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
//------------------------------------------------------------------------------

#if DESKTOP

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Forms;
using Microsoft.Identity.Core.UI;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class WinFormAuthTests
    {
        [TestMethod]
        public void WindowsFormsWebAuthenticationDialog_FormPostToUrlTest()
        {
            // Arrange
            string htmlResponse = "<html><head><title>Working...</title></head><body><form method=\"POST\" name=\"hiddenform\" action=\"https://ResponseUri\"><input type=\"hidden\" name=\"code\" value=\"someAuthCodeValueInFormPost\" /><input type=\"hidden\" name=\"session_state\" value=\"9f0efc27-15c0-45e9-be87-d11d81d913a8\" /><noscript><p>Script is disabled. Click Submit to continue.</p><input type=\"submit\" value=\"Submit\" /></noscript></form><script language=\"javascript\">document.forms[0].submit();</script></body></html>";
            WebBrowser browser = new WebBrowser();
            browser.DocumentText = htmlResponse;
            browser.Document.Write(htmlResponse);

            // Act
            string url = WindowsFormsWebAuthenticationDialogBase.GetUrlFromDocument(
                new Uri("https://mocktest.net/callback"),
                browser.Document);

            // Assert
            var result = new AuthorizationResult(AuthorizationStatus.Success, url);
            Assert.AreEqual("someAuthCodeValueInFormPost", result.Code);
        }
    }
}
#endif    

