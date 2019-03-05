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

using System;
using System.IO;
using System.Reflection;
using CommonCache.Test.Common;

namespace CommonCache.Test.Unit.Utils
{
    public static class CacheProgramFactory
    {
        private static string BaseExecutablePath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static CacheProgram CreateCacheProgram(CacheProgramType cacheProgramType, CacheStorageType cacheStorageType)
        {
            string executablePath;
            string resultsFilePath;

            switch (cacheProgramType)
            {
            case CacheProgramType.AdalV3:
                executablePath = Path.Combine(BaseExecutablePath, "AdalV3", "CommonCache.Test.AdalV3.exe");
                resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "adalv3results.json");
                break;
            case CacheProgramType.AdalV4:
                executablePath = Path.Combine(BaseExecutablePath, "AdalV4", "CommonCache.Test.AdalV4.exe");
                resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "adalv4results.json");
                break;
            case CacheProgramType.AdalV5:
                executablePath = Path.Combine(BaseExecutablePath, "AdalV5", "CommonCache.Test.AdalV5.exe");
                resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "adalv5results.json");
                break;
            case CacheProgramType.MsalV2:
                executablePath = Path.Combine(BaseExecutablePath, "MsalV2", "CommonCache.Test.MsalV2.exe");
                resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "msalv2results.json");
                break;
            case CacheProgramType.MsalV3:
                executablePath = Path.Combine(BaseExecutablePath, "MsalV3", "CommonCache.Test.MsalV3.exe");
                resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "msalv3results.json");
                break;
            default:
                throw new ArgumentException("Unknown cacheProgramType", nameof(cacheProgramType));
            }

            return new CacheProgram(executablePath, resultsFilePath, cacheStorageType);
        }
    }
}
