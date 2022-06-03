// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    class TestIdentityLogger : IIdentityLogger
    {
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return true;
        }

        public void Log(LogEntry entry)
        {
            StringBuilder.AppendLine(entry.Message);
        }
    }
}
