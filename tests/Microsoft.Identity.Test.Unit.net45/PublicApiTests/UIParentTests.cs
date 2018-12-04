//----------------------------------------------------------------------
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
using Microsoft.Identity.Client.Platforms.Android;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class UIParentTests
    {
#if NET_CORE
        [TestMethod]
        public void UIParent_ThrowsOnNetCore()
        {
            AssertException.Throws<PlatformNotSupportedException>(() => new UIParent());
            AssertException.Throws<PlatformNotSupportedException>(() => new UIParent("parent", true));

        }

        [TestMethod]
        public void UIParent_IsSystemAvailable()
        {
             Assert.IsFalse(UIParent.IsSystemWebviewAvailable());
        }
#endif

#if DESKTOP
        [TestMethod]
        public void UIParent_EmptyCtor()
        {
            UIParent uiParent = new UIParent();

            Assert.IsFalse(uiParent.UseHiddenBrowser);
            Assert.IsNotNull(uiParent.CoreUIParent);
            Assert.IsNull(uiParent.CoreUIParent.OwnerWindow);
            Assert.IsFalse(uiParent.CoreUIParent.UseHiddenBrowser);

        }

        [TestMethod]
        public void UIParent_NetstndardCtor()
        {
            object parent = "parent";
            UIParent uiParent = new UIParent(parent, true);

            Assert.IsFalse(uiParent.UseHiddenBrowser);
            Assert.IsFalse(uiParent.CoreUIParent.UseHiddenBrowser);

            uiParent.UseHiddenBrowser = true;

            Assert.IsTrue(uiParent.UseHiddenBrowser);
            Assert.IsTrue(uiParent.CoreUIParent.UseHiddenBrowser);
            Assert.AreSame(parent, uiParent.CoreUIParent.OwnerWindow);
        }

        [TestMethod]
        public void IsSystemWebview()
        {
            Assert.IsFalse(UIParent.IsSystemWebviewAvailable());
        }
#endif
    }
}
