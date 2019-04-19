// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace CommonCache.Test.Common
{
    public class CacheExecutorResults
    {
        public CacheExecutorResults()
        {
        }

        public List<CacheExecutorAccountResult> AccountResults { get; set; } = new List<CacheExecutorAccountResult>();
    }
}
