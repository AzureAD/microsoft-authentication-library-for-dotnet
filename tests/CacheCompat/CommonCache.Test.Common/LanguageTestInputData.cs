// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CommonCache.Test.Common
{
    internal class LanguageTestInputData : TestInputData
    {
        public LanguageTestInputData(TestInputData testInputData, string scope, string cacheFilePath)
        {
            LabUserDatas = testInputData.LabUserDatas;
            ResultsFilePath = testInputData.ResultsFilePath;
            StorageType = testInputData.StorageType;
            Scope = scope;
            CacheFilePath = cacheFilePath;
        }

        public string Scope { get; }
        public string CacheFilePath { get; }
    }
}
