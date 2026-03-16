// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// A simple <see cref="IIdentityLogger"/> implementation suitable for use in tests.
    /// All log entries are written to <see cref="Trace"/> output.
    /// </summary>
    public class TestIdentityLogger : IIdentityLogger
    {
        private readonly EventLogLevel _minLevel;

        /// <summary>Initializes a new instance with the given minimum log level.</summary>
        public TestIdentityLogger(EventLogLevel minLevel = EventLogLevel.Verbose)
        {
            _minLevel = minLevel;
        }

        /// <inheritdoc />
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel >= _minLevel;
        }

        /// <inheritdoc />
        public void Log(LogEntry entry)
        {
            if (entry == null) return;
            Trace.WriteLine($"[MSAL][{entry.EventLogLevel}] {entry.Message}");
        }
    }
}
