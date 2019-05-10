// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
