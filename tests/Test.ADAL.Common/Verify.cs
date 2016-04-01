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

using System;
#if TEST_ADAL_WINRT_UNIT
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.ADAL.Common
{
    static class Verify
    {
        internal static void IsGreaterThanOrEqual(IComparable expectedGreater, IComparable expectedLess, string message = null)
        {
            Assert.IsTrue(expectedGreater.CompareTo(expectedLess) >= 0, message);
        }

        internal static void IsLessThanOrEqual(IComparable expectedLess, IComparable expectedGreater, string message = null)
        {
            Assert.IsTrue(expectedLess.CompareTo(expectedGreater) <= 0, message);
        }

        internal static void AreEqual(IComparable expected, IComparable actual, string message = null)
        {
            Assert.IsTrue((expected == null && actual == null) || expected.CompareTo(actual) == 0, message);
        }

        internal static void AreEqual(object expected, object actual, string message = null)
        {
            Assert.IsTrue(expected == actual || (expected != null && expected.Equals(actual)), message);
        }

        internal static void AreNotEqual(object expected, object actual, string message = null)
        {
            Assert.IsFalse(expected == actual || (expected != null && expected.Equals(actual)), message);
        }

        internal static void IsTrue(bool condition, string message = null)
        {
            Assert.IsTrue(condition, message);
        }

        internal static void IsFalse(bool condition, string message = null)
        {
            Assert.IsFalse(condition, message);
        }

        internal static void IsNull(object variable, string message = null)
        {
            Assert.IsNull(variable, message);
        }

        internal static void IsNotNull(object variable, string message = null)
        {
            Assert.IsNotNull(variable, message);
        }

        internal static void IsNullOrEmptyString(string variable, string message = null)
        {
            Assert.IsTrue(string.IsNullOrEmpty(variable), message);
        }

        internal static void IsNotNullOrEmptyString(string variable, string message = null)
        {
            Assert.IsFalse(string.IsNullOrEmpty(variable), message);
        }

        internal static void Fail(string message, params object[] args)
        {
            Assert.IsTrue(false, message, args);
        }
    }
}
