// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CommonCache.Test.Common
{
    public class CacheExecutorAccountResult
    {
        public CacheExecutorAccountResult()
        {
        }

        public CacheExecutorAccountResult(string labUserUpn, string authResultUpn, bool isAuthResultFromCache)
        {
            LabUserUpn = labUserUpn;
            AuthResultUpn = authResultUpn;
            IsAuthResultFromCache = isAuthResultFromCache;
        }

        public string LabUserUpn { get; set; }
        public string AuthResultUpn { get; set; }
        public bool IsAuthResultFromCache { get; set; }
    }
}
