// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class NullLogger : ICoreLogger
    {
        public string ClientName { get; } = string.Empty;
        public string ClientVersion { get; } = string.Empty;

        public Guid CorrelationId { get; } = Guid.Empty;
        public bool PiiLoggingEnabled { get; } = false;

        public void Always(string messageScrubbed)
        {
        }

        public void AlwaysPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void Error(string messageScrubbed)
        {
        }

        public void ErrorPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void ErrorPii(Exception exWithPii)
        {
        }

        public void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Warning(string messageScrubbed)
        {
        }

        public void WarningPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void WarningPii(Exception exWithPii)
        {
        }

        public void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Info(string messageScrubbed)
        {
        }

        public void InfoPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void InfoPii(Exception exWithPii)
        {
        }

        public void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Verbose(string messageScrubbed)
        {
        }

        public void VerbosePii(string messageWithPii, string messageScrubbed)
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
