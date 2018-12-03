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

using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    [TestClass]
    public class FileSystemCredentialPathManagerTests
    {
        private readonly FileSystemCredentialPathManager _credentialPathManager = new FileSystemCredentialPathManager();

        [TestMethod]
        public void ToSafeFilename()
        {
            Assert.AreEqual("98JPIEIUEFT7FFJK", _credentialPathManager.ToSafeFilename("!@#$%^&*()-+"));
            Assert.AreEqual("SEOC8GKOVGE196NR", _credentialPathManager.ToSafeFilename(""));
            Assert.AreEqual("82E183VGAG9CFOF4", _credentialPathManager.ToSafeFilename("=^^="));
            Assert.AreEqual("EOE7CM5P6N5I6EAS", _credentialPathManager.ToSafeFilename("alreadySafeButStill"));
            Assert.AreEqual("EOE7CM5P6N5I6EAS", _credentialPathManager.ToSafeFilename("AlReAdYsAfEbUtStIlL"));
            Assert.AreEqual(
                "EPGP81EH0BA8BLKC",
                _credentialPathManager.ToSafeFilename("================================================"));
        }
    }
}