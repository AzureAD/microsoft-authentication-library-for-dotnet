// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    public static class HashSetUtil
    {
        public static bool AreEqual(HashSet<string> expected, HashSet<string> actual)
        {
            if (expected == null && actual == null)
            {
                return true;
            }

            if (expected == null || actual == null)
            {
                return false;
            }

            foreach (string key in expected)
            {
                if (!actual.Contains(key))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
