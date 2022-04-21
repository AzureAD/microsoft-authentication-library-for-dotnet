// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
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

        string ILoggerAdapter.CorrelationId { get; } = string.Empty;

        public string ClientInformation { get; } = string.Empty;

        public bool IsDefaultPlatformLoggingEnabled { get; } = false;

        public bool IsLoggingEnabled(EventLevel logLevel)
        {
            return false;
        }

        public void Log(EventLevel logLevel, string messageWithPii, string messageScrubbed)
        {
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, EventLevel logLevel = EventLevel.Verbose)
        {
            return null;
        }

        public DurationLogHelper LogMethodDuration(EventLevel logLevel = EventLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return null;
        }
    }
}
