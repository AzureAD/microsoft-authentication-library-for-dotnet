// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class CallbackIdentityLogger : IIdentityLogger
    {
        private LogCallback _logCallback;
        private readonly string _correlationId;
        private readonly string _clientInformation;
        private readonly bool _piiLoggingEnabled;
        private readonly LogLevel _minLogLevel;

        public CallbackIdentityLogger(
            LogCallback logCallback,
            string correlationId,
            string clientName,
            string clientVersion,
            bool enablePiiLogging,
            LogLevel minLogLevel)
        {
            _correlationId = correlationId;
            _clientInformation = LoggerHelper.GetClientInfo(clientName, clientVersion);
            _piiLoggingEnabled = enablePiiLogging;
            _logCallback = logCallback;
            _minLogLevel = minLogLevel;
        }

        public bool IsEnabled(EventLogLevel eventLevel)
        {
            return _logCallback != null && GetLogLevel(eventLevel) <= _minLogLevel;
        }

        public void Log(LogEntry entry)
        {
            string formattedMessage = LoggerHelper.FormatLogMessage(
                                            entry.Message,
                                            _piiLoggingEnabled,
                                            string.IsNullOrEmpty(entry.CorrelationId) ? entry.CorrelationId : _correlationId,
                                            _clientInformation);

            _logCallback.Invoke(GetLogLevel(entry.EventLogLevel), formattedMessage, _piiLoggingEnabled);
        }

        private static LogLevel GetLogLevel(EventLogLevel eventLogLevel)
        {
            //MSAL does not have a critical log level so it is combined with the error level
            if (eventLogLevel == EventLogLevel.LogAlways)
            {
                return LogLevel.Always;
            }
            return (LogLevel)((int)eventLogLevel - 2);
        }
    }
}
