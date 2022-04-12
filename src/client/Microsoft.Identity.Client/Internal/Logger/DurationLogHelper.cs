// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal sealed class DurationLogHelper : IDisposable
    {
        private readonly ILoggerAdapter _logger;
        private readonly string _measuredBlockName;
        private readonly LogLevel _logLevel;
        private readonly Stopwatch _stopwatch;

        public DurationLogHelper(
            ILoggerAdapter logger,
            string measuredBlockName,
            LogLevel logLevel = LogLevel.Verbose)
        {
            _logger = logger;
            _measuredBlockName = measuredBlockName;
            _logLevel = logLevel;
            _stopwatch = Stopwatch.StartNew();

            logger.Log(new LogEntry()
            {
                Message = $"Starting {measuredBlockName}",
                EventLevel = EventLevel.Verbose
            });
        }

        public void Dispose()
        {
            _logger.Log(new LogEntry()
            {
                Message = $"Finished {_measuredBlockName} in {_stopwatch.ElapsedMilliseconds} ms",
                EventLevel = EventLevel.Verbose
            });
        }
    }
}
