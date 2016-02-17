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

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.Common
{
    public static class CommonUnitTests
    {
        public static void CreateSha256HashTest()
        {
            ICryptographyHelper cryptoHelper = new CryptographyHelper();
            string hash = cryptoHelper.CreateSha256Hash("abc");
            string hash2 = cryptoHelper.CreateSha256Hash("abd");
            string hash3 = cryptoHelper.CreateSha256Hash("abc");
            Assert.AreEqual(hash, hash3);
            Assert.AreNotEqual(hash, hash2);
            Assert.AreEqual(hash, "ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=");
        }

        public static void AdalIdTest()
        {
            var adalParameters = MsalIdHelper.GetAdalIdParameters();

            Assert.AreEqual(4, adalParameters.Count);
            Assert.IsNotNull(adalParameters[AdalIdParameter.Product]);
            Assert.IsNotNull(adalParameters[AdalIdParameter.Version]);
            Assert.IsNotNull(adalParameters[AdalIdParameter.CpuPlatform]);
#if TEST_ADAL_WINRT_UNIT
            Assert.IsFalse(adalParameters.ContainsKey(AdalIdParameter.OS));
            Assert.IsNotNull(adalParameters[AdalIdParameter.DeviceModel]);
#else
            Assert.IsNotNull(adalParameters[AdalIdParameter.OS]);
            Assert.IsFalse(adalParameters.ContainsKey(AdalIdParameter.DeviceModel));
#endif

            var parameters = new DictionaryRequestParameters(null, new ClientKey("client_id"));
            adalParameters = MsalIdHelper.GetAdalIdParameters();

            Assert.AreEqual(4, adalParameters.Count);
            Assert.IsNotNull(adalParameters[AdalIdParameter.Product]);
            Assert.IsNotNull(adalParameters[AdalIdParameter.Version]);
            Assert.IsNotNull(adalParameters[AdalIdParameter.CpuPlatform]);
#if TEST_ADAL_WINRT_UNIT
            Assert.IsFalse(adalParameters.ContainsKey(AdalIdParameter.OS));
            Assert.IsNotNull(adalParameters[AdalIdParameter.DeviceModel]);
#else
            Assert.IsNotNull(adalParameters[AdalIdParameter.OS]);
            Assert.IsFalse(adalParameters.ContainsKey(AdalIdParameter.DeviceModel));
#endif
        }
    }
}
