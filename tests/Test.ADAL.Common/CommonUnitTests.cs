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
            IHttpClient request = PlatformPlugin.HttpClientFactory.Create("https://test", null);
            AdalIdHelper.AddAsHeaders(request.Headers);

            Verify.AreEqual(4, request.Headers.Count);
            Verify.IsNotNull(request.Headers[AdalIdParameter.Product]);
            Verify.IsNotNull(request.Headers[AdalIdParameter.Version]);
            Verify.IsNotNull(request.Headers[AdalIdParameter.CpuPlatform]);
#if TEST_ADAL_WINRT_UNIT
            Verify.IsFalse(request.Headers.ContainsKey(AdalIdParameter.OS));
            Verify.IsNotNull(request.Headers[AdalIdParameter.DeviceModel]);
#else
            Verify.IsNotNull(request.Headers[AdalIdParameter.OS]);
            Verify.IsFalse(request.Headers.ContainsKey(AdalIdParameter.DeviceModel));
#endif

            RequestParameters parameters = new RequestParameters(null, new ClientKey("client_id"));
            AdalIdHelper.AddAsQueryParameters(parameters);

            Verify.AreEqual(5, parameters.Count);
            Verify.IsNotNull(parameters[AdalIdParameter.Product]);
            Verify.IsNotNull(parameters[AdalIdParameter.Version]);
            Verify.IsNotNull(parameters[AdalIdParameter.CpuPlatform]);
#if TEST_ADAL_WINRT_UNIT
            Verify.IsFalse(parameters.ContainsKey(AdalIdParameter.OS));
            Verify.IsNotNull(parameters[AdalIdParameter.DeviceModel]);
#else
            Verify.IsNotNull(parameters[AdalIdParameter.OS]);
            Verify.IsFalse(parameters.ContainsKey(AdalIdParameter.DeviceModel));
#endif
        }
    }
}
