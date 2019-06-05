// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CommonCache.Test.Common
{
    internal class LanguageTestInputData : TestInputData
    {
        public LanguageTestInputData(TestInputData testInputData)
        {
            LabUserDatas = testInputData.LabUserDatas;
            ResultsFilePath = testInputData.ResultsFilePath;
            StorageType = testInputData.StorageType;
        }

        public string ClientId { get; set; }
        public string Authority { get; set; }
        public string Scope { get; set; }
        public string CacheFilePath { get; set; }
    }
}
