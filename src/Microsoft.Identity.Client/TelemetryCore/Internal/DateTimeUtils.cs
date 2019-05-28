// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal static class DateTimeUtils
    {
        public static long GetMillisecondsSinceEpoch(DateTime timeUtc)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((timeUtc - epoch).TotalMilliseconds);
        }
    }
}
