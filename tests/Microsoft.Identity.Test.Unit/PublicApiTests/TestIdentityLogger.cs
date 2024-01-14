// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    class TestIdentityLogger : IIdentityLogger
    {
        public EventLogLevel MinLogLevel { get; }

        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public TestIdentityLogger(EventLogLevel logLevel = EventLogLevel.Verbose)
        {
            MinLogLevel = logLevel;
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        public void Log(LogEntry entry)
        {
            StringBuilder.AppendLine(entry.Message);
        }
    }
}
