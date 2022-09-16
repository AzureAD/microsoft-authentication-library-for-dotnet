// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class NullLogger : ILoggerAdapter
    {
        public string ClientName { get; } = string.Empty;
        public string ClientVersion { get; } = string.Empty;

        public Guid CorrelationId { get; } = Guid.Empty;

        public bool PiiLoggingEnabled { get; } = false;

        public string ClientInformation { get; } = string.Empty;

        public bool IsDefaultPlatformLoggingEnabled { get; } = false;

        public IIdentityLogger IdentityLogger { get; } = NullIdentityModelLogger.Instance;

        public bool IsLoggingEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed)
        {
            //No op
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return null;
        }

        public DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return null;
        }
    }
}
