// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class NullLogger : IMsalLogger
    {
        public string ClientName { get; } = string.Empty;
        public string ClientVersion { get; } = string.Empty;

        public Guid CorrelationId { get; } = Guid.Empty;
        public bool PiiLoggingEnabled { get; } = false;

        public void Log(LogLevel logLevel, string messageScrubbed)
        {
        }

        public void Log(LogLevel msalLogLevel, string messageWithPii, string messageScrubbed)
        {
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(this, measuredBlockName, logLevel);
        }

        public DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return LogBlockDuration(methodName, logLevel);
        }

        public bool IsLoggingEnabled(LogLevel logLevel)
        {
            return false;
        }
    }
}
