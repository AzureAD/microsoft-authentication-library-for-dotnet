// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client.Core
{
    /// <summary>
    /// Extension methods for ILoggerAdapter
    /// </summary>
    internal static class MsalLoggerExtensions
    {
        public static void Always(this ILoggerAdapter logger, string message)
        {
            logger.Log(LogLevel.Always, string.Empty, message);
        }

        public static void AlwaysPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Always, messageWithPii, messageScrubbed);
        }

        public static void Error(this ILoggerAdapter logger, string message)
        {
            logger.Log(LogLevel.Error, string.Empty, message);
        }

        public static void ErrorPiiWithPrefix(this ILoggerAdapter logger, Exception exWithPii, string prefix)
        {
            logger.Log(LogLevel.Error, prefix + exWithPii.ToString(), prefix + LoggerHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void ErrorPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Error, messageWithPii, messageScrubbed);
        }

        public static void ErrorPii(this ILoggerAdapter logger, Exception exWithPii)
        {
            logger.Log(LogLevel.Error, exWithPii.ToString(), LoggerHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Warning(this ILoggerAdapter logger, string message)
        {
            logger.Log(LogLevel.Warning, string.Empty, message);
        }

        public static void WarningPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Warning, messageWithPii, messageScrubbed);
        }

        public static void WarningPii(this ILoggerAdapter logger, Exception exWithPii)
        {
            logger.Log(LogLevel.Warning, exWithPii.ToString(), LoggerHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void WarningPiiWithPrefix(this ILoggerAdapter logger, Exception exWithPii, string prefix)
        {
            logger.Log(LogLevel.Warning, prefix + exWithPii.ToString(), prefix + LoggerHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Info(this ILoggerAdapter logger, string message)
        {
            logger.Log(LogLevel.Info, string.Empty, message);
        }

        public static void InfoPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Info, messageWithPii, messageScrubbed);
        }

        public static void InfoPii(this ILoggerAdapter logger, Exception exWithPii)
        {
            logger.Log(LogLevel.Info, exWithPii?.ToString(), LoggerHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void InfoPiiWithPrefix(this ILoggerAdapter logger, Exception exWithPii, string prefix)
        {
            logger.Log(LogLevel.Info, prefix + exWithPii.ToString(), prefix + LoggerHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Verbose(this ILoggerAdapter logger, string message)
        {
            logger.Log(LogLevel.Verbose, string.Empty, message);
        }

        public static void VerbosePii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(LogLevel.Verbose, messageWithPii, messageScrubbed);
        }
    }
}
