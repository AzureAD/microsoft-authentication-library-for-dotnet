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

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    public static class CommonUnitTests
    {
        public static void CreateSha256HashTest()
        {
            ICryptographyHelper cryptoHelper = new CryptographyHelper();
            string hash = cryptoHelper.CreateSha256Hash("abc");
            string hash2 = cryptoHelper.CreateSha256Hash("abd");
            string hash3 = cryptoHelper.CreateSha256Hash("abc");
            Verify.AreEqual(hash, hash3);
            Verify.AreNotEqual(hash, hash2);
            Verify.AreEqual(hash, "ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=");
        }

        public static void AdalIdTest()
        {
            var adalParameters = MsalIdHelper.GetAdalIdParameters();

            Verify.AreEqual(4, adalParameters.Count);
            Verify.IsNotNull(adalParameters[AdalIdParameter.Product]);
            Verify.IsNotNull(adalParameters[AdalIdParameter.Version]);
            Verify.IsNotNull(adalParameters[AdalIdParameter.CpuPlatform]);
#if TEST_ADAL_WINRT_UNIT
            Verify.IsFalse(adalParameters.ContainsKey(AdalIdParameter.OS));
            Verify.IsNotNull(adalParameters[AdalIdParameter.DeviceModel]);
#else
            Verify.IsNotNull(adalParameters[AdalIdParameter.OS]);
            Verify.IsFalse(adalParameters.ContainsKey(AdalIdParameter.DeviceModel));
#endif

            var parameters = new DictionaryRequestParameters(null, new ClientKey("client_id"));
            adalParameters = MsalIdHelper.GetAdalIdParameters();

            Verify.AreEqual(4, adalParameters.Count);
            Verify.IsNotNull(adalParameters[AdalIdParameter.Product]);
            Verify.IsNotNull(adalParameters[AdalIdParameter.Version]);
            Verify.IsNotNull(adalParameters[AdalIdParameter.CpuPlatform]);
#if TEST_ADAL_WINRT_UNIT
            Verify.IsFalse(adalParameters.ContainsKey(AdalIdParameter.OS));
            Verify.IsNotNull(adalParameters[AdalIdParameter.DeviceModel]);
#else
            Verify.IsNotNull(adalParameters[AdalIdParameter.OS]);
            Verify.IsFalse(adalParameters.ContainsKey(AdalIdParameter.DeviceModel));
#endif
        }
    }
}
