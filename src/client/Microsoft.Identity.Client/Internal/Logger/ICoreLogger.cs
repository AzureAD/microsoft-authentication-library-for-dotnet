// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client.Core
{
    internal interface ICoreLogger
    {
        string ClientName { get; }
        string ClientVersion { get; }
        bool PiiLoggingEnabled { get; }
        void Always(string messageScrubbed);
        void AlwaysPii(string messageWithPii, string messageScrubbed);
        void Error(string messageScrubbed);
        void ErrorPii(string messageWithPii, string messageScrubbed);
        void ErrorPii(Exception exWithPii);
        void ErrorPiiWithPrefix(Exception exWithPii, string prefix);
        void Warning(string messageScrubbed);
        void WarningPii(string messageWithPii, string messageScrubbed);
        void WarningPii(Exception exWithPii);
        void WarningPiiWithPrefix(Exception exWithPii, string prefix);
        void Info(string messageScrubbed);
        void InfoPii(string messageWithPii, string messageScrubbed);
        void InfoPii(Exception exWithPii);
        void InfoPiiWithPrefix(Exception exWithPii, string prefix);
        void Verbose(string messageScrubbed);
        void VerbosePii(string messageWithPii, string messageScrubbed);
        void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed);

        /// <summary>
        /// For expensive logging messages (e.g. when the log message evaluates a variable), 
        /// it is better to check the log level ahead of time so as not to evaluate the expensive message and then discard it.
        /// </summary>
        bool IsLoggingEnabled(LogLevel logLevel);
        DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose);
        DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null);
    }
}
