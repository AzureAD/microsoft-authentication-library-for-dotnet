// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// A simple <see cref="IIdentityLogger"/> implementation suitable for use in tests.
    /// All log entries are written to <see cref="Trace"/> output and also appended to
    /// <see cref="StringBuilder"/> for assertion in unit tests.
    /// </summary>
    public class TestIdentityLogger : IIdentityLogger
    {
        private readonly EventLogLevel _minLevel;

        /// <summary>Initializes a new instance with the given minimum log level.</summary>
        public TestIdentityLogger(EventLogLevel minLevel = EventLogLevel.Verbose)
        {
            _minLevel = minLevel;
        }

        /// <summary>
        /// Accumulates every logged message so tests can assert on captured output.
        /// </summary>
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="EventLogLevel.Verbose"/> == 5 is the highest (least-urgent) level.
        /// Returning <c>true</c> when <paramref name="eventLogLevel"/> &lt;= <see cref="_minLevel"/>
        /// means "enable all levels up to and including the configured minimum", i.e. with the
        /// default of <see cref="EventLogLevel.Verbose"/> every message is enabled.
        /// </remarks>
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= _minLevel;
        }

        /// <inheritdoc />
        public void Log(LogEntry entry)
        {
            if (entry == null) return;
            Trace.WriteLine($"[MSAL][{entry.EventLogLevel}] {entry.Message}");
            StringBuilder.AppendLine(entry.Message);
        }
    }
}
