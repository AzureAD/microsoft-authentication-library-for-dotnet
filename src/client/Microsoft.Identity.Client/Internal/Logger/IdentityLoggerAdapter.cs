// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
#if !XAMARINMAC20
    internal class IdentityLoggerAdapter : ILoggerAdapter
    {
        private readonly IIdentityLogger _identityLogger;
        private string _clientInfo;
        private string _correlationId;

        public bool PiiLoggingEnabled { get; }
        public bool IsDefaultPlatformLoggingEnabled { get; } = false;
        public string ClientName { get; }
        public string ClientVersion { get; }

        internal IdentityLoggerAdapter(
            IIdentityLogger identityLogger,
            Guid correlationId,
            string clientName,
            string clientVersion,
            bool enablePiiLogging)
        {
            ClientName = clientName;
            ClientVersion = clientVersion;
            _identityLogger = identityLogger;
            _correlationId = correlationId.Equals(Guid.Empty)
                    ? string.Empty
                    : " - " + correlationId;

            _clientInfo = LoggerAdapterHelper.GetClientInfo(clientName, clientVersion);
            
            PiiLoggingEnabled = enablePiiLogging;
        }

        public static ILoggerAdapter Create(
            Guid correlationId,
            ApplicationConfiguration config)
        {
            return new IdentityLoggerAdapter(
                config?.IdentityLogger,
                correlationId,
                config?.ClientName ?? string.Empty,
                config?.ClientVersion ?? string.Empty,
                config?.EnablePiiLogging ?? false);
        }

        public void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed)
        {
            LogEntry entry = Log(this, logLevel, messageWithPii, messageScrubbed);
            _identityLogger.Log(entry);
        }

        public LogEntry Log(ILoggerAdapter logger, LogLevel logLevel, string messageWithPii, string messageScrubbed)
        {
            LogEntry entry = null;

            if (logger.IsLoggingEnabled(logLevel))
            {
                entry = new LogEntry();
                entry.EventLogLevel = GetEventLogLevel(logLevel);
                entry.CorrelationId = _correlationId;
                entry.Message = LoggerAdapterHelper.FormatLogMessage(messageWithPii, messageScrubbed, logger.PiiLoggingEnabled, _correlationId, _clientInfo);
            }

            return entry;
        }

        public bool IsLoggingEnabled(LogLevel logLevel)
        {
            return _identityLogger.IsEnabled(GetEventLogLevel(logLevel));
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return LoggerAdapterHelper.LogBlockDuration(this, measuredBlockName, logLevel);
        }

        public DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return LoggerAdapterHelper.LogMethodDuration(this, logLevel, methodName, filePath);
        }

        public EventLogLevel GetEventLogLevel(LogLevel logLevel)
        {
            return (EventLogLevel)((int)logLevel + 1);
        }
    }
#endif
}
