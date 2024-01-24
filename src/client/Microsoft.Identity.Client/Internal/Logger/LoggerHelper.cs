// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class LoggerHelper
    {
        private static Lazy<string> s_msalVersionLazy = new Lazy<string>(MsalIdHelper.GetMsalVersion);
        private static Lazy<string> s_runtimeVersionLazy = new Lazy<string>(() => PlatformProxyFactory.CreatePlatformProxy(null).GetRuntimeVersion());
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
            ApplicationConfiguration config)
        {
            if (config.IdentityLogger == null)
            {
                if (config.LoggingCallback == null)
                {
                    return s_nullLogger.Value;
                }

                return CallbackIdentityLoggerAdapter.Create(correlationId, config);
            }

#if XAMARINMAC20
            throw new NotImplementedException();
#else
            return IdentityLoggerAdapter.Create(correlationId, config);
#endif
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

        public static string FormatLogMessage(string message, bool piiEnabled, string correlationId, string clientInformation)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} MSAL {1} {2} {3} {4} [{5}{6}]{7} {8}",
                piiEnabled,
                s_msalVersionLazy.Value,
                s_skuLazy.Value,
                s_runtimeVersionLazy.Value,
                s_osLazy.Value,
                DateTime.UtcNow.ToString("u"),
                correlationId,
                clientInformation,
                message);
        }

        internal static string GetPiiScrubbedExceptionDetails(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Exception type: {0}", ex.GetType()));

            if (ex is MsalException msalException)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, ", ErrorCode: {0}", msalException.ErrorCode));
            }

            if (ex is MsalServiceException msalServiceException)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "HTTP StatusCode {0}", msalServiceException.StatusCode));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "CorrelationId {0}", msalServiceException.CorrelationId));
                if (msalServiceException.ErrorCodes != null && msalServiceException.ErrorCodes.Count() > 0)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Microsoft Entra ID Error Code AADSTS{0}", string.Join(" ", msalServiceException.ErrorCodes)));
                }
            }

            if (ex.InnerException != null)
            {
                sb.AppendLine("---> Inner Exception Details");
                sb.AppendLine(GetPiiScrubbedExceptionDetails(ex.InnerException));
                sb.AppendLine("=== End of inner exception stack trace ===");
            }

            sb.AppendLine("To see full exception details, enable PII Logging. See https://aka.ms/msal-net-logging");

            if (ex.StackTrace != null)
            {
                sb.AppendLine(ex.StackTrace);
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
            return new DurationLogHelper(logger, fileName + ":" + methodName, logLevel);
        }

        public static EventLogLevel GetEventLogLevel(LogLevel logLevel)
        {
            //MSAL does not have a critical log level so it is combined with the error level
            if (logLevel == LogLevel.Always)
            {
                return EventLogLevel.LogAlways;
            }

            return (EventLogLevel)((int)logLevel + 2);
        }

        public static string GetMessageToLog(string messageWithPii, string messageScrubbed, bool piiLoggingEnabled)
        {
            bool messageWithPiiExists = !string.IsNullOrWhiteSpace(messageWithPii);
            // If we have a message with PII, and PII logging is enabled, use the PII message, else use the scrubbed message.
            bool isLoggingPii = messageWithPiiExists && piiLoggingEnabled;
            return isLoggingPii ? messageWithPii : messageScrubbed;
        }
    }
}
