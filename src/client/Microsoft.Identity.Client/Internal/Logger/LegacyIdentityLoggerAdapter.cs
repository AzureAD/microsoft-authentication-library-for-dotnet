// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class LegacyIdentityLoggerAdapter : IIdentityLogger
    {
        LogLevel _minLogLevel = LogLevel.Always;
        LogCallback _logCallback;

        public bool IsPiiEnabled => throw new NotImplementedException();

        public bool IsEnabled(EventLevel eventLevel)
        {
            return GetLegacyLogLevel(eventLevel) <= _minLogLevel;
        }

        public void Log(LogEntry logEntry)
        {
            InvokeLogCallback(logEntry);
        }

        public void LogWithPii(LogEntry logEntry)
        {
            InvokeLogCallback(logEntry, true);
        }

        private void InvokeLogCallback(LogEntry logEntry, bool containsPii = false)
        {
            _logCallback.Invoke(GetLegacyLogLevel(logEntry.EventLevel), logEntry.Message, containsPii);
        }

        public LegacyIdentityLoggerAdapter(LogLevel logLevel, LogCallback logCallback)
        {
            _minLogLevel = logLevel;
            _logCallback = logCallback;
        }

        private LogLevel GetLegacyLogLevel(EventLevel eventLevel)
        {
            return (LogLevel)((int)eventLevel - 1);
        }
    }
}
