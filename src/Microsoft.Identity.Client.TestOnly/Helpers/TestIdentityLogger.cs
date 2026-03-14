// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// A simple <see cref="IIdentityLogger"/> implementation that accumulates log messages in a
    /// <see cref="StringBuilder"/> for use in tests.
    /// </summary>
    /// <example>
    /// <code>
    /// var logger = new TestIdentityLogger(EventLogLevel.Verbose);
    ///
    /// var app = PublicClientApplicationBuilder
    ///     .Create("client-id")
    ///     .WithLogging(logger)
    ///     .Build();
    ///
    /// // ... execute flow ...
    ///
    /// Assert.Contains("expected log text", logger.StringBuilder.ToString());
    /// </code>
    /// </example>
    public class TestIdentityLogger : IIdentityLogger
    {
        /// <summary>Gets the minimum log level that this logger records (inclusive).</summary>
        public EventLogLevel MinLogLevel { get; }

        /// <summary>Gets the buffer holding all logged messages.</summary>
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of <see cref="TestIdentityLogger"/>.
        /// </summary>
        /// <param name="logLevel">
        /// The minimum <see cref="EventLogLevel"/> to capture.
        /// Defaults to <see cref="EventLogLevel.Verbose"/> (capture everything).
        /// </param>
        public TestIdentityLogger(EventLogLevel logLevel = EventLogLevel.Verbose)
        {
            MinLogLevel = logLevel;
        }

        /// <inheritdoc/>
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        /// <inheritdoc/>
        public void Log(LogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            StringBuilder.AppendLine(entry.Message);
        }
    }
}
