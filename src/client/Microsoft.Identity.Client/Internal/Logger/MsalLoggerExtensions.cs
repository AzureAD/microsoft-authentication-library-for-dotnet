// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for ICoreLogger
    /// </summary>
    internal static class MsalLoggerExtensions
    {
        public static void Info(this ICoreLogger logger, string messageScrubbed)
        {
            logger.Log(LogLevel.Info, string.Empty, messageScrubbed);
        }

        public static void InfoPii(this ICoreLogger logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Info, messageWithPii, messageScrubbed);
        }

        public static void InfoPii(this ICoreLogger logger, Exception exWithPii)
        {
            logger.Log(LogLevel.Info, exWithPii.ToString(), MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void InfoPiiWithPrefix(this ICoreLogger logger, Exception exWithPii, string prefix)
        {
            logger.Log(LogLevel.Info, prefix + exWithPii.ToString(), prefix + MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Verbose(this ICoreLogger logger, string messageScrubbed)
        {
            logger.Log(LogLevel.Verbose, string.Empty, messageScrubbed);
        }

        public static void VerbosePii(this ICoreLogger logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Verbose, messageWithPii, messageScrubbed);
        }

        public static void Warning(this ICoreLogger logger, string messageScrubbed)
        {
            logger.Log(LogLevel.Warning, string.Empty, messageScrubbed);
        }

        public static void WarningPii(this ICoreLogger logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Warning, messageWithPii, messageScrubbed);
        }

        public static void WarningPii(this ICoreLogger logger, Exception exWithPii)
        {
            logger.Log(LogLevel.Warning, exWithPii.ToString(), MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void WarningPiiWithPrefix(this ICoreLogger logger, Exception exWithPii, string prefix)
        {
            logger.Log(LogLevel.Warning, prefix + exWithPii.ToString(), prefix + MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Error(this ICoreLogger logger, string messageScrubbed)
        {
            logger.Log(LogLevel.Error, string.Empty, messageScrubbed);
        }

        public static void ErrorPiiWithPrefix(this ICoreLogger logger, Exception exWithPii, string prefix)
        {
            logger.Log(LogLevel.Error, prefix + exWithPii.ToString(), prefix + MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void ErrorPii(this ICoreLogger logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Error, messageWithPii, messageScrubbed);
        }

        public static void ErrorPii(this ICoreLogger logger, Exception exWithPii)
        {
            logger.Log(LogLevel.Error, exWithPii.ToString(), MsalLogger.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Always(this ICoreLogger logger, string messageScrubbed)
        {
            logger.Log(LogLevel.Always, string.Empty, messageScrubbed);
        }

        public static void AlwaysPii(this ICoreLogger logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Always, messageWithPii, messageScrubbed);
        }

        public static DurationLogHelper LogBlockDuration(this ICoreLogger logger, string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(logger, measuredBlockName, logLevel);
        }

        public static DurationLogHelper LogMethodDuration(this ICoreLogger logger, LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            string fileName = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "";
            return LogBlockDuration(logger, fileName + ":" + methodName, logLevel);
        }
    }
}
