// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
                case CacheProgramType.MsalPython:
                    executablePath = Path.Combine(BaseExecutablePath, "MsalPython", "CommonCache.Test.MsalPython.exe");
                    resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "msal_python_results.json");
                    break;
                case CacheProgramType.MsalJava:
                    executablePath = Path.Combine(BaseExecutablePath, "MsalJava", "CommonCache.Test.MsalJava.exe");
                    resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "msal_java_results.json");
                    break;
                case CacheProgramType.MsalNode:
                    executablePath = Path.Combine(BaseExecutablePath, "MsalNode", "CommonCache.Test.MsalNode.exe");
                    resultsFilePath = Path.Combine(CommonCacheTestUtils.CacheFileDirectory, "msal_node_results.json");
                    break;
                default:
                    throw new ArgumentException("Unknown cacheProgramType", nameof(cacheProgramType));
            }

            return new CacheProgram(executablePath, resultsFilePath, cacheStorageType);
        }
    }
}
