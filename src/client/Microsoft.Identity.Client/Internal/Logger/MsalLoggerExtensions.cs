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
            logger.Log(EventLevel.LogAlways, string.Empty, message);
        }

        public static void AlwaysPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(EventLevel.LogAlways, messageWithPii, messageScrubbed);
        }

        public static void Error(this ILoggerAdapter logger, string message)
        {
            logger.Log(EventLevel.Error, string.Empty, message);
        }

        public static void ErrorPiiWithPrefix(this ILoggerAdapter logger, Exception exWithPii, string prefix)
        {
            logger.Log(EventLevel.Error, prefix + exWithPii.ToString(), prefix + LoggerAdapterHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void ErrorPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(EventLevel.Error, messageWithPii, messageScrubbed);
        }

        public static void ErrorPii(this ILoggerAdapter logger, Exception exWithPii)
        {
            logger.Log(EventLevel.Error, exWithPii.ToString(), LoggerAdapterHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Warning(this ILoggerAdapter logger, string message)
        {
            logger.Log(EventLevel.Warning, string.Empty, message);
        }

        public static void WarningPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(EventLevel.Warning, messageWithPii, messageScrubbed);
        }

        public static void WarningPii(this ILoggerAdapter logger, Exception exWithPii)
        {
            logger.Log(EventLevel.Warning, exWithPii.ToString(), LoggerAdapterHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void WarningPiiWithPrefix(this ILoggerAdapter logger, Exception exWithPii, string prefix)
        {
            logger.Log(EventLevel.Warning, prefix + exWithPii.ToString(), prefix + LoggerAdapterHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Info(this ILoggerAdapter logger, string message)
        {
            logger.Log(EventLevel.Informational, string.Empty, message);
        }

        public static void InfoPii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(EventLevel.Informational, messageWithPii, messageScrubbed);
        }

        public static void InfoPii(this ILoggerAdapter logger, Exception exWithPii)
        {
            logger.Log(EventLevel.Informational, exWithPii.ToString(), LoggerAdapterHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void InfoPiiWithPrefix(this ILoggerAdapter logger, Exception exWithPii, string prefix)
        {
            logger.Log(EventLevel.Informational, prefix + exWithPii.ToString(), prefix + LoggerAdapterHelper.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public static void Verbose(this ILoggerAdapter logger, string message)
        {
            logger.Log(EventLevel.Verbose, string.Empty, message);
        }

        public static void VerbosePii(this ILoggerAdapter logger, string messageWithPii, string messageScrubbed)
        {
            logger.Log(EventLevel.Verbose, messageWithPii, messageScrubbed);
        }
    }
}
