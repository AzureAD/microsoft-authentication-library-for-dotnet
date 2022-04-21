// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class LegacyIdentityLoggerAdapter : ILoggerAdapter
    {
        LogLevel _minLogLevel = LogLevel.Always;
        LogCallback _logCallback;
        private LoggerAdapterHelper _loggerAdapterHelper;

        public bool PiiLoggingEnabled { get; }
        public bool IsDefaultPlatformLoggingEnabled { get; }

        public MsalCacheLoggerWrapper CacheLogger => throw new NotImplementedException();

        public bool IsLoggingEnabled(EventLevel eventLevel)
        {
            return _loggerAdapterHelper.GetLegacyLogLevel(eventLevel) <= _minLogLevel;
        }

        internal LegacyIdentityLoggerAdapter(
            Guid correlationId,
            string clientName,
            string clientVersion,
            LogLevel logLevel,
            bool enablePiiLogging,
            bool isDefaultPlatformLoggingEnabled,
            LogCallback loggingCallback)
        {
            var _correlationId = correlationId.Equals(Guid.Empty)
                    ? string.Empty
                    : " - " + correlationId;

            _loggerAdapterHelper = new LoggerAdapterHelper(_correlationId, clientName, clientVersion);

            PiiLoggingEnabled = enablePiiLogging;
            _logCallback = loggingCallback;
            _minLogLevel = logLevel;
            IsDefaultPlatformLoggingEnabled = isDefaultPlatformLoggingEnabled;
        }

        public void Log(EventLevel logLevel, string messageWithPii, string messageScrubbed)
        {
            LogEntry entry = _loggerAdapterHelper.Log(this, logLevel, messageWithPii, messageScrubbed);
            //LogToDefaultPlatformLogger(logger, logLevel, log);
            InvokeLogCallback(entry, string.IsNullOrEmpty(messageWithPii)? true: false);
        }

        private void InvokeLogCallback(LogEntry logEntry, bool containsPii = false)
        {
            _logCallback.Invoke(_loggerAdapterHelper.GetLegacyLogLevel(logEntry.EventLevel), logEntry.Message, containsPii);
        }

        //private static void LogToDefaultPlatformLogger(ILoggerAdapter logger, EventLevel logLevel, string log)
        //{
        //    if (logger.IsDefaultPlatformLoggingEnabled)
        //    {
        //        switch (logLevel)
        //        {
        //            case EventLevel.LogAlways:
        //                _platformLogger.Always(log);
        //                break;
        //            case EventLevel.Error:
        //                _platformLogger.Error(log);
        //                break;
        //            case EventLevel.Warning:
        //                _platformLogger.Warning(log);
        //                break;
        //            case EventLevel.Informational:
        //                _platformLogger.Information(log);
        //                break;
        //            case EventLevel.Verbose:
        //                _platformLogger.Verbose(log);
        //                break;
        //        }
        //    }
        //}

        public static ILoggerAdapter Create(
            Guid correlationId,
            ApplicationConfiguration config,
            bool isDefaultPlatformLoggingEnabled = false)
        {
            return new LegacyIdentityLoggerAdapter(
                correlationId,
                config?.ClientName ?? string.Empty,
                config?.ClientVersion ?? string.Empty,
                config?.LogLevel ?? LogLevel.Verbose,
                config?.EnablePiiLogging ?? false,
                config?.IsDefaultPlatformLoggingEnabled ?? isDefaultPlatformLoggingEnabled,
                config?.LoggingCallback);
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, EventLevel logLevel = EventLevel.Verbose)
        {
            return _loggerAdapterHelper.LogBlockDuration(this, measuredBlockName, logLevel);
        }

        public DurationLogHelper LogMethodDuration(EventLevel logLevel = EventLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return _loggerAdapterHelper.LogMethodDuration(this, logLevel, methodName, filePath);
        }
    }
}
