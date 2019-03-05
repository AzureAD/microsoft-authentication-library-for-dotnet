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

using System.IO;

namespace CommonCache.Test.Common
{
    public static class CommonCacheTestUtils
    {
        public static string CacheFileDirectory => Path.Combine(Path.GetTempPath(), "adalcachecompattestdata");
        public static string AdalV3CacheFilePath => Path.Combine(CacheFileDirectory, "cacheAdalV3.bin");
        public static string MsalV2CacheFilePath => Path.Combine(CacheFileDirectory, "msalCacheV2.bin");
        public static string MsalV3CacheFilePath => Path.Combine(CacheFileDirectory, "msalCacheV3.bin");

        public static void EnsureCacheFileDirectoryExists()
        {
            Directory.CreateDirectory(CacheFileDirectory);
        }

        public static void DeleteAllTestCaches()
        {
            if (File.Exists(AdalV3CacheFilePath))
            {
                File.Delete(AdalV3CacheFilePath);
            }

            if (File.Exists(MsalV2CacheFilePath))
            {
                File.Delete(MsalV2CacheFilePath);
            }

            if (File.Exists(MsalV3CacheFilePath))
            {
                File.Delete(MsalV3CacheFilePath);
            }
        }
    }
}
