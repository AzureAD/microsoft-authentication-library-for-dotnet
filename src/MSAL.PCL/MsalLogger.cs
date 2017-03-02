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

        // There is a circular dependency between PLatformPlugin and MsalLogger.
        // It's not possible to log messages to platform specific logs prior to the initialization of the plugin.
        // Currently, the Logger is being called if an error occurs during plugin initialization.

        //static platform plugin

        #region LogMessages

        public static void Error(string message)
        {
            // Errors are always logged, but in the future an even more restrictive level
            // could potentially be added, so might as well keep the guard condition.
            if (LogLevel.Error > ApplicationLogLevel) return;
            string formattedMessage = LogMessage(message, LogLevel.Error);
            PlatformPlugin.Logger.Error(formattedMessage);
        }

        public static void Warning(string message)
        {
            if (LogLevel.Warning > ApplicationLogLevel) return;
            string formattedMessage = LogMessage(message, LogLevel.Warning);
            PlatformPlugin.Logger.Warning(formattedMessage);
        }
        public static void Info(string message)
        {
            if (LogLevel.Info > ApplicationLogLevel) return;
            string formattedMessage = LogMessage(message, LogLevel.Info);
            PlatformPlugin.Logger.Information(formattedMessage);
        }
        public static void Verbose(string message)
        {
            if (LogLevel.Verbose > ApplicationLogLevel) return;
            string formattedMessage = LogMessage(message, LogLevel.Verbose);
            PlatformPlugin.Logger.Verbose(formattedMessage);
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

        private static string LogMessage(string logMessage, LogLevel logLevel)
        {
            //format;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? string.Empty
                : CorrelationId.ToString();
            string log = string.Format(CultureInfo.CurrentCulture, "{0}: {1}: {2}", DateTime.UtcNow, correlationId,
                logMessage);
            //platformPlugin

            //callback();
            LoggerCallbackHandler.ExecuteCallback(logLevel, log);

            return log;
        }
    }
}