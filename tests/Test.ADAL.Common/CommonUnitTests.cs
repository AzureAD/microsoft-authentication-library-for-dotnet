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
            var adalParameters = AdalIdHelper.GetAdalIdParameters();

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
            adalParameters = AdalIdHelper.GetAdalIdParameters();

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
