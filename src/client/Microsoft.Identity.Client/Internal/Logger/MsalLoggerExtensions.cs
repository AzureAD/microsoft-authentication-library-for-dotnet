// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for ICoreLogger
    /// </summary>
    internal static class MsalLoggerExtensions
    {
        public static void Always(this IIdentityLogger logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.LogAlways;

            logger.Log(logEntry);
        }

        public static void AlwaysPii(this IIdentityLogger logger, string messageWithPii, string messageScrubbed)
        {
            var logEntry = new LogEntry();
            logEntry.Message = logger.IsPiiEnabled ? messageWithPii: messageScrubbed;
            logEntry.EventLevel = EventLevel.LogAlways;

            logger.LogToAvailableLogger(logEntry, true);
        }

        public static void Error(this IIdentityLogger logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Error;

            logger.Log(logEntry);
        }

        public static void Warning(this IIdentityLogger logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Warning;

            logger.Log(logEntry);
        }

        public static void Info(this IIdentityLogger logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Informational;

            logger.Log(logEntry);
        }

        public static void InfoPii(this IIdentityLogger logger, string messageWithPii, string messageScrubbed)
        {
            var logEntry = new LogEntry();
            logEntry.Message = logger.IsPiiEnabled ? messageWithPii : messageScrubbed;
            logEntry.EventLevel = EventLevel.Informational;

            logger.LogToAvailableLogger(logEntry, true);
        }

        public static void Verbose(this IIdentityLogger logger, string message)
        {
            var logEntry = new LogEntry();
            logEntry.Message = message;
            logEntry.EventLevel = EventLevel.Verbose;

            logger.Log(logEntry);
        }

        //Since there is not a good way to determine if the pii value is needed by the logger at the moment, we need to make a check.
        private static void LogToAvailableLogger(this IIdentityLogger logger, LogEntry logEntry, bool containsPii)
        {
            //Not the best way to do this. Needs refactoring.
            if (containsPii && logger is LegacyIdentityLoggerAdapter loggerAdapter)
            {
                loggerAdapter.LogWithPii(logEntry);
            }
            else
            {
                logger.Log(logEntry);
            }
        }

        //public static void InfoPii(this IIdentityLogger logger, Exception exWithPii)
        //{
        //    logger.Log(LogLevel.Info, exWithPii.ToString(), MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        //}

        //public static void InfoPiiWithPrefix(this IIdentityLogger logger, Exception exWithPii, string prefix)
        //{
        //    logger.Log(LogLevel.Info, prefix + exWithPii.ToString(), prefix + MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        //}

        //public static void VerbosePii(this IIdentityLogger logger, string messageWithPii, string messageScrubbed)
        //{
        //    logger.Log(LogLevel.Verbose, messageWithPii, messageScrubbed);
        //}

        //public static void WarningPii(this IIdentityLogger logger, string messageWithPii, string messageScrubbed)
        //{
        //    logger.Log(LogLevel.Warning, messageWithPii, messageScrubbed);
        //}

        //public static void WarningPii(this IIdentityLogger logger, Exception exWithPii)
        //{
        //    logger.Log(LogLevel.Warning, exWithPii.ToString(), MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        //}

        //public static void WarningPiiWithPrefix(this IIdentityLogger logger, Exception exWithPii, string prefix)
        //{
        //    logger.Log(LogLevel.Warning, prefix + exWithPii.ToString(), prefix + MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        //}

        //public static void ErrorPiiWithPrefix(this IIdentityLogger logger, Exception exWithPii, string prefix)
        //{
        //    logger.Log(LogLevel.Error, prefix + exWithPii.ToString(), prefix + MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        //}

        //public static void ErrorPii(this IIdentityLogger logger, string messageWithPii, string messageScrubbed)
        //{
        //    logger.Log(LogLevel.Error, messageWithPii, messageScrubbed);
        //}

        //public static void ErrorPii(this IIdentityLogger logger, Exception exWithPii)
        //{
        //    logger.Log(LogLevel.Error, exWithPii.ToString(), MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        //}

        //public static void AlwaysPii(this IIdentityLogger logger, string messageWithPii, string messageScrubbed)
        //{
        //    logger.Log(LogLevel.Always, messageWithPii, messageScrubbed);
        //}

        public static DurationLogHelper LogBlockDuration(this IIdentityLogger logger, string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(logger, measuredBlockName, logLevel);
        }

        public static DurationLogHelper LogMethodDuration(this IIdentityLogger logger, LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            string fileName = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "";
            return LogBlockDuration(logger, fileName + ":" + methodName, logLevel);
        }
    }
}
