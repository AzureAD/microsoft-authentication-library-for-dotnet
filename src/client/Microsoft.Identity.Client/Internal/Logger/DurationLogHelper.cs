// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal sealed class DurationLogHelper : IDisposable
    {
        private readonly ICoreLogger _logger;
        private readonly string _measuredBlockName;
        private readonly LogLevel _logLevel;
        private readonly Stopwatch _stopwatch;

        public DurationLogHelper(
            ICoreLogger logger,
            string measuredBlockName,
            LogLevel logLevel = LogLevel.Verbose)
        {
            _logger = logger;
            _measuredBlockName = measuredBlockName;
            _logLevel = logLevel;
            _stopwatch = Stopwatch.StartNew();

            logger.Log(logLevel, null, $"Starting {measuredBlockName}");
        }

        public void Dispose()
        {
            _logger.Log(_logLevel, null, $"Finished {_measuredBlockName} in {_stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
