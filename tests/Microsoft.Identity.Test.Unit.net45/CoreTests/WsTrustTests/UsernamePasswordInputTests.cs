//----------------------------------------------------------------------
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

using System.Linq;
using System.Security;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
#if !ANDROID && !iOS && !WINDOWS_APP
    [TestClass]
    public class UsernamePasswordInputTests
    {
        [TestMethod]
        public void PlainTextPassword()
        {
            // Arrange
            UsernamePasswordInput input = new UsernamePasswordInput("user", "plain_text_password");

            // Act 
            char[] charPassword = input.PasswordToCharArray();

            // Assert
            Assert.IsTrue(input.HasPassword());
            CollectionAssert.AreEqual("plain_text_password".ToCharArray(), charPassword);
        }

#if DESKTOP // no explicit support for netcore on ADAL
        [TestMethod]
        public void SecureStringPassword()
        {
            // Arrange
            SecureString secureString = new SecureString();
            "secure_string_password".ToCharArray().ToList().ForEach(c => secureString.AppendChar(c));
            UsernamePasswordInput input = new UsernamePasswordInput("user", secureString);

            // Act 
            char[] charPassword = input.PasswordToCharArray();

            // Assert
            Assert.IsTrue(input.HasPassword());
            CollectionAssert.AreEqual("secure_string_password".ToCharArray(), charPassword);
        }
#endif
    }
#endif
}
