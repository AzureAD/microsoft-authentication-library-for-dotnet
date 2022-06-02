// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class LoggerAdapterHelper
    {
        private static Lazy<string> s_msalVersionLazy = new Lazy<string>(() => MsalIdHelper.GetMsalVersion());
        private static Lazy<string> s_runtimeVersionLazy = new Lazy<string>(() => PlatformProxyFactory.CreatePlatformProxy(null).GetRuntimeVersion());
        //private static IPlatformLogger _platformLogger = PlatformProxyFactory.CreatePlatformProxy(null).PlatformLogger;
        private static readonly Lazy<ILoggerAdapter> s_nullLogger = new Lazy<ILoggerAdapter>(() => new NullLogger());

        public static string GetClientInfo(string clientName, string clientVersion)
        {
            string clientInformation = string.Empty;
            if (!string.IsNullOrEmpty(clientName) && !ApplicationConfiguration.DefaultClientName.Equals(clientName))
            {
                // space is intentional for formatting of the message
                if (string.IsNullOrEmpty(clientVersion))
                {
                    clientInformation = string.Format(CultureInfo.InvariantCulture, " ({0})", clientName);
                }
                else
                {
                    clientInformation = string.Format(CultureInfo.InvariantCulture, " ({0}: {1})", clientName, clientVersion);
                }
            }

            return clientInformation;
        }

        public static ILoggerAdapter CreateLogger(
            Guid correlationId,
            ApplicationConfiguration config,
            bool isDefaultPlatformLoggingEnabled = false)
        {
            if (config.IdentityLogger == null)
            {
                return LegacyIdentityLoggerAdapter.Create(correlationId, config, isDefaultPlatformLoggingEnabled);
            }

            return IdentityLoggerAdapter.Create(correlationId, config);
        }

        public static ILoggerAdapter NullLogger => s_nullLogger.Value;

        private static Lazy<string> s_osLazy = new Lazy<string>(() =>
        {
            if (MsalIdHelper.GetMsalIdParameters(null).TryGetValue(MsalIdParameter.OS, out string osValue))
            {
                return osValue;
            }
            return "Unknown OS";
        });

        private static Lazy<string> s_skuLazy = new Lazy<string>(() =>
        {
            if (MsalIdHelper.GetMsalIdParameters(null).TryGetValue(MsalIdParameter.Product, out string sku))
            {
                return sku;
            }
            return "Unknown SKU";
        });

        public static string FormatLogMessage(string messageWithPii, string messageScrubbed, bool piiEnabled, string correlationId, string clientInformation)
        {
            bool messageWithPiiExists = !string.IsNullOrWhiteSpace(messageWithPii);
            // If we have a message with PII, and PII logging is enabled, use the PII message, else use the scrubbed message.
            bool isLoggingPii = messageWithPiiExists && piiEnabled;
            string messageToLog = isLoggingPii ? messageWithPii : messageScrubbed;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} MSAL {1} {2} {3} {4} [{5}{6}]{7} {8}",
                isLoggingPii,
                s_msalVersionLazy.Value,
                s_skuLazy.Value,
                s_runtimeVersionLazy.Value,
                s_osLazy.Value,
                DateTime.UtcNow.ToString("u"),
                correlationId,
                clientInformation,
                messageToLog);
        }

        internal static string GetPiiScrubbedExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex != null)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Exception type: {0}", ex.GetType()));

                if (ex is MsalException msalException)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, ", ErrorCode: {0}", msalException.ErrorCode));
                }

                if (ex is MsalServiceException msalServiceException)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "HTTP StatusCode {0}", msalServiceException.StatusCode));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "CorrelationId {0}", msalServiceException.CorrelationId));
                }

                if (ex.InnerException != null)
                {
                    sb.AppendLine("---> Inner Exception Details");
                    sb.AppendLine(GetPiiScrubbedExceptionDetails(ex.InnerException));
                    sb.AppendLine("=== End of inner exception stack trace ===");
                }

                if (ex.StackTrace != null)
                {
                    sb.Append(Environment.NewLine + ex.StackTrace);
                }
            }

            return sb.ToString();
        }

        public static DurationLogHelper LogBlockDuration(ILoggerAdapter logger, string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(logger, measuredBlockName, logLevel);
        }

        public static DurationLogHelper LogMethodDuration(ILoggerAdapter logger, LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            string fileName = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "";
            return LogBlockDuration(logger, fileName + ":" + methodName, logLevel);
        }
    }
}
