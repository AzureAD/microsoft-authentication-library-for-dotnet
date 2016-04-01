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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Test.ADAL.Common;
using Test.ADAL.Common.Unit;

namespace Test.ADAL.WinRT.Unit
{
    [TestClass]
    public class TokenCacheUnitTests
    {
        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test to store in default token cache")]
        public void DefaultTokenCacheTest()
        {
            TokenCacheTests.DefaultTokenCacheTest();
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for Cache Operations")]
        public void TokenCacheOperationsTest()
        {
            TokenCacheTests.TokenCacheOperationsTest();
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for Token Cache Capacity")]
        public void TokenCacheCapacityTest()
        {
            TokenCacheTests.TokenCacheCapacityTest();
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for Token Cache Value Split")]
        public void TokenCacheValueSplitTest()
        {
            TokenCacheTests.TokenCacheValueSplitTest();
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for Token Cache Encryption")]
        public void TokenCacheEncryptionTest()
        {
            TestEncryption(null);
            TestEncryption(string.Empty);
            TestEncryption(" ");
            TestEncryption("This is a test message");
            TestEncryption("asdfk+j0a-=skjwe43;1l234 1#$!$#%345903485qrq@#$!@#$!(rekr341!#$%Ekfaآزمايشsdsdfsddfdgsfgjsglk==CVADS");
            TestEncryption(@"a\u0304\u0308"" = ""ā̈");
            TestEncryption(TokenCacheTests.GenerateRandomString(10000));

            try
            {
                CryptographyHelper.Decrypt("آزمايش");
                Verify.Fail("Exception expected");
            }
            catch (FormatException)
            {
                // Expected
            }
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for Token Cache Serialization")]
        public void TokenCacheSerializationTest()
        {
            TokenCacheTests.TokenCacheSerializationTest();
        }
    
        public static void TestEncryption(string message)
        {
            string encryptedMessage = CryptographyHelper.Encrypt(message);
            if (!string.IsNullOrEmpty(message))
            {
                Verify.AreNotEqual(message, encryptedMessage);
            }

            string decryptedMessage = CryptographyHelper.Decrypt(encryptedMessage);
            Verify.AreEqual(message, decryptedMessage);
        }
    }
}
