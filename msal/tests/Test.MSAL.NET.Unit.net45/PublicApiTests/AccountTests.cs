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

using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class AccountTests
    {
        [TestMethod]
        [TestCategory("UserTests")]
        public void Constructor_IdIsNotRequired()
        {
            // 1. Id not is required
            new Account(null, "d", "n");

            // 2. Other properties are optional too
            new Account("a.b", null, null);
        }

        [TestMethod]
        [TestCategory("UserTests")]
        public void Constructor_PropertiesSet()
        {
            Account actual = new Account("a.b", "disp", "env");

            Assert.AreEqual("a.b", actual.HomeAccountId.Identifier);
            Assert.AreEqual("a", actual.HomeAccountId.ObjectId);
            Assert.AreEqual("b", actual.HomeAccountId.TenantId);
            Assert.AreEqual("disp", actual.Username);
            Assert.AreEqual("env", actual.Environment);
        }
    }
}
