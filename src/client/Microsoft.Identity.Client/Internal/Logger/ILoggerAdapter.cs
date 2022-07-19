// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Core
{
    internal interface ILoggerAdapter
    {
        bool PiiLoggingEnabled { get; }
        bool IsDefaultPlatformLoggingEnabled { get; }
        string ClientName { get; }
        string ClientVersion { get; }
        IIdentityLogger IdentityLogger { get; }

        /// <summary>
        /// For expensive logging messages (e.g. when the log message evaluates a variable), 
        /// it is better to check the log level ahead of time so as not to evaluate the expensive message and then discard it.
        /// </summary>
        bool IsLoggingEnabled(LogLevel logLevel);
        void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed);
        DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose);
        DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null);
    }
}
