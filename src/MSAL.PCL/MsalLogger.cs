using System;
using System.Globalization;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public class MsalLogger
    {
        public enum LogLevel
        {
            Error = 0,
            Warning = 1,
            Info = 2,
            Verbose = 3
        }

        internal MsalLogger(Guid correlationId)
        {
            this.CorrelationId = correlationId;
        }

        internal MsalLogger()
        {

        }

        internal MsalLogger(RequestContext requestContext)
        {
            if (requestContext != null)
            {
                this.CorrelationId = Guid.Parse(requestContext.CorrelationId);
            }
        }

        // CorrelationId on the logger is set when the correlationId on the client application is set.
        internal Guid CorrelationId { get; set; }

        internal LogLevel ApplicationLogLevel { get; set; } = LogLevel.Info;

        #region LogMessages

        public void Error(string message)
        {
            LogMessage(message, LogLevel.Error);
        }
        public void Warning(string message)
        {
            LogMessage(message, LogLevel.Warning);
        }
        public void Info(string message)
        {
            LogMessage(message, LogLevel.Info);
        }
        public void Verbose(string message)
        {
            LogMessage(message, LogLevel.Verbose);
        }

        public void Error(Exception ex)
        {
            Error(ex.ToString());
        }
        public void Warning(Exception ex)
        {
            Warning(ex.ToString());
        }
        public void Info(Exception ex)
        {
            Info(ex.ToString());
        }
        public void Verbose(Exception ex)
        {
            Verbose(ex.ToString());
        }

        #endregion

        private void LogMessage(string logMessage, LogLevel logLevel)
        {
            if (logLevel > ApplicationLogLevel) return;

            //format log message;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? "No CorrelationId"
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