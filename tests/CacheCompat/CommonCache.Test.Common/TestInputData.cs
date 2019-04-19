using System;
using System.Collections.Generic;
using System.Text;

namespace CommonCache.Test.Common
{
    public class TestInputData
    {
        public List<LabUserData> LabUserDatas { get; set; }
        public string ResultsFilePath { get; set; }
        public CacheStorageType StorageType { get; set; }
    }
}
