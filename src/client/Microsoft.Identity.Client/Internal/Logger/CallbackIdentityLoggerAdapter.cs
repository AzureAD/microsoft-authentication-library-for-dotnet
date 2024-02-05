// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class CallbackIdentityLoggerAdapter : ILoggerAdapter
    {
        private string _correlationId;

        public bool PiiLoggingEnabled { get; }
        public bool IsDefaultPlatformLoggingEnabled { get; }
        public string ClientName { get; }
        public string ClientVersion { get; }
        public IIdentityLogger IdentityLogger { get; }

        public bool IsLoggingEnabled(LogLevel logLevel)
        {
            return IdentityLogger.IsEnabled(LoggerHelper.GetEventLogLevel(logLevel));
        }

        internal CallbackIdentityLoggerAdapter(
            Guid correlationId,
            string clientName,
            string clientVersion,
            LogLevel logLevel,
            bool enablePiiLogging,
            bool isDefaultPlatformLoggingEnabled,
            LogCallback loggingCallback)
        {
            ClientName = clientName;
            ClientVersion = clientVersion;
            _correlationId = correlationId.Equals(Guid.Empty)
                    ? string.Empty
                    : " - " + correlationId;

            PiiLoggingEnabled = enablePiiLogging;
            IsDefaultPlatformLoggingEnabled = isDefaultPlatformLoggingEnabled;
            IdentityLogger = new CallbackIdentityLogger(loggingCallback, _correlationId, clientName, clientVersion, enablePiiLogging, logLevel);
        }

        public void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed)
        {
            if (IsLoggingEnabled(logLevel))
            {
                string messageToLog = LoggerHelper.GetMessageToLog(messageWithPii, messageScrubbed, PiiLoggingEnabled);

                LogEntry entry = new LogEntry();
                entry.EventLogLevel = LoggerHelper.GetEventLogLevel(logLevel);
                entry.CorrelationId = _correlationId;
                entry.Message = messageToLog;
                IdentityLogger.Log(entry);
            }
        }

        public static ILoggerAdapter Create(
            Guid correlationId,
            ApplicationConfiguration config,
            bool isDefaultPlatformLoggingEnabled = false)
        {
            return new CallbackIdentityLoggerAdapter(
                correlationId,
                config?.ClientName ?? string.Empty,
                config?.ClientVersion ?? string.Empty,
                config?.LogLevel ?? LogLevel.Verbose,
                config?.EnablePiiLogging ?? false,
                config?.IsDefaultPlatformLoggingEnabled ?? isDefaultPlatformLoggingEnabled,
                config?.LoggingCallback);
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(this, measuredBlockName, logLevel);
        }

        public DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return LoggerHelper.LogMethodDuration(this, logLevel, methodName, filePath);
        }
    }
}
