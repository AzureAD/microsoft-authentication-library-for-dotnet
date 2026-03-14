// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TestOnly.Logging
{
    /// <summary>
    /// A simple <see cref="IIdentityLogger"/> implementation suitable for tests.
    /// Appends log entries to an in-memory <see cref="StringBuilder"/> for later inspection.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// var logger = new TestIdentityLogger(EventLogLevel.Verbose);
    /// var app = PublicClientApplicationBuilder
    ///     .Create("client-id")
    ///     .WithLogging(logger)
    ///     .Build();
    /// // …run test…
    /// Assert.Contains("expected text", logger.StringBuilder.ToString());
    /// </code>
    /// </remarks>
    public sealed class TestIdentityLogger : IIdentityLogger
    {
        /// <summary>
        /// Gets the minimum log level at which entries are accepted.
        /// Default is <see cref="EventLogLevel.Verbose"/>.
        /// </summary>
        public EventLogLevel MinLogLevel { get; }

        /// <summary>Gets the in-memory buffer that accumulates log messages.</summary>
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        /// <summary>
        /// Initializes a new <see cref="TestIdentityLogger"/>.
        /// </summary>
        /// <param name="logLevel">
        /// The minimum log level. Events at this level or higher are recorded.
        /// Defaults to <see cref="EventLogLevel.Verbose"/> (capture everything).
        /// </param>
        public TestIdentityLogger(EventLogLevel logLevel = EventLogLevel.Verbose)
        {
            MinLogLevel = logLevel;
        }

        /// <inheritdoc />
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        /// <inheritdoc />
        public void Log(LogEntry entry)
        {
            StringBuilder.AppendLine(entry?.Message);
        }
    }
}
