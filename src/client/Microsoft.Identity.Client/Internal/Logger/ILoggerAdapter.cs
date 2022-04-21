// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client.Core
{
    internal interface ILoggerAdapter
    {
        bool PiiLoggingEnabled { get; }
        bool IsDefaultPlatformLoggingEnabled { get; }
        MsalCacheLoggerWrapper CacheLogger { get; }

        /// <summary>
        /// For expensive logging messsages (e.g. when the log message evaluates a variable), 
        /// it is better to check the log level ahead of time so as not to evaluate the expensive message and then discard it.
        /// </summary>
        bool IsLoggingEnabled(EventLevel eventLevel);
        void Log(EventLevel logLevel, string messageWithPii, string messageScrubbed);
        DurationLogHelper LogBlockDuration(string measuredBlockName, EventLevel logLevel = EventLevel.Verbose);
        DurationLogHelper LogMethodDuration(EventLevel logLevel = EventLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null);
    }
}
