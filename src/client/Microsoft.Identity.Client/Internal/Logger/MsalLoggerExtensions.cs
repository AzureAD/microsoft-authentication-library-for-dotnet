// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Internal.Logger.LogScrubber;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for ICoreLogger
    /// </summary>
    internal static class MsalLoggerExtensions
    {
        public static void Always(this ILoggerAdapter logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.LogAlways;

            logger.Log(logEntry);
        }

        public static void Error(this ILoggerAdapter logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Error;

            logger.Log(logEntry);
        }

        public static void Warning(this ILoggerAdapter logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Warning;

            logger.Log(logEntry);
        }

        public static void Info(this ILoggerAdapter logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Informational;

            logger.Log(logEntry);
        }

        public static void Verbose(this ILoggerAdapter logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Verbose;

            logger.Log(logEntry);
        }

        public static DurationLogHelper LogBlockDuration(this ILoggerAdapter logger, string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(logger, measuredBlockName, logLevel);
        }

        public static DurationLogHelper LogMethodDuration(this ILoggerAdapter logger, LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            string fileName = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "";
            return LogBlockDuration(logger, fileName + ":" + methodName, logLevel);
        }
    }
}
