// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class TimeConstants
    {
        public const int ActionTimeoutMilliseconds = 600000;
        public const int ScenarioTimeoutMilliseconds = 900000;
        public const int AggregationWindowMilliseconds = 30000;
    }
}
