// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal sealed class DurationLogHelper : IDisposable
    {
        private readonly ILoggerAdapter _logger;
        private readonly string _measuredBlockName;
        private readonly EventLevel _logLevel;
        private readonly Stopwatch _stopwatch;

        public DurationLogHelper(
            ILoggerAdapter logger,
            string measuredBlockName,
            EventLevel logLevel = EventLevel.Verbose)
        {
            _logger = logger;
            _measuredBlockName = measuredBlockName;
            _logLevel = logLevel;
            _stopwatch = Stopwatch.StartNew();

            _logger.Log(EventLevel.Verbose, string.Empty, $"Starting {measuredBlockName}");
        }

        public void Dispose()
        {
            _logger.Log(EventLevel.Verbose, string.Empty, $"Finished {_measuredBlockName} in {_stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
