//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

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
        //[Description("Test for TokenCache")]
        public async Task TokenCacheKeyTest()
        {
            await TokenCacheTests.TokenCacheKeyTestAsync();
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
        //[Description("Test for Token Cache Capacity")]
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
