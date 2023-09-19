// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal class OpenTelemetryFactory
    {
        public static IOpenTelemetryClient CreateClient()
        {
#if NETSTANDARD || NET6_0 || NET462
            return new OpenTelemetryClient();
#else
            return null;
#endif
        }
    }
}
