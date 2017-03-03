using System;
using System.Globalization;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public static class MsalLogger
    {
        public enum LogLevel
        {
            Error = 0,
            Warning = 1,
            Info = 2,
            Verbose = 3
        }

        // CorrelationId on the logger is set when the correlationId on the client application is set.
        internal static Guid CorrelationId { get; set; }

        public static LogLevel ApplicationLogLevel { get; set; } = LogLevel.Info;

        #region LogMessages

        public static void Error(string message)
        {
            LogMessage(message, LogLevel.Error);
        }
        public static void Warning(string message)
        {
            LogMessage(message, LogLevel.Warning);
        }
        public static void Info(string message)
        {
            LogMessage(message, LogLevel.Info);
        }
        public static void Verbose(string message)
        {
            LogMessage(message, LogLevel.Verbose);
        }

        public static void Error(Exception ex)
        {
            Error(ex.ToString());
        }
        public static void Warning(Exception ex)
        {
            Warning(ex.ToString());
        }
        public static void Info(Exception ex)
        {
            Info(ex.ToString());
        }
        public static void Verbose(Exception ex)
        {
            Verbose(ex.ToString());
        }

        #endregion

        private static void LogMessage(string logMessage, LogLevel logLevel)
        {
            if (logLevel > ApplicationLogLevel) return;

            //format log message;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? string.Empty
                : CorrelationId.ToString();
            string log = string.Format(CultureInfo.CurrentCulture, "{0}: {1}: {2}", DateTime.UtcNow, correlationId,
                logMessage);

            //platformPlugin
            PlatformPlugin.LogMessage(logLevel, log);

            //callback();
            LoggerCallbackHandler.ExecuteCallback(logLevel, log);
        }
    }
}