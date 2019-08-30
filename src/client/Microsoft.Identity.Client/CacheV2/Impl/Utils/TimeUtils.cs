// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.CacheV2.Impl.Utils
{
    internal static class TimeUtils
    {
        public static long GetSecondsFromEpochNow()
        {
            var t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToInt64(t.TotalSeconds);
        }
    }
}
