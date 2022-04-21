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
    internal class IdentityLoggerAdapter : ILoggerAdapter
    {
        private readonly IIdentityLogger _identityLogger;
        private LoggerAdapterHelper _loggerAdapterHelper;

        public bool PiiLoggingEnabled { get; }
        public bool IsDefaultPlatformLoggingEnabled { get; } = false;
        public MsalCacheLoggerWrapper CacheLogger { get; }

        internal IdentityLoggerAdapter(
            IIdentityLogger identityLogger,
            Guid correlationId,
            string clientName,
            string clientVersion,
            bool enablePiiLogging)
        {
            _identityLogger = identityLogger;
            var _correlationId = correlationId.Equals(Guid.Empty)
                    ? string.Empty
                    : " - " + correlationId;
            _loggerAdapterHelper = new LoggerAdapterHelper(_correlationId, clientName, clientVersion);
            
            PiiLoggingEnabled = enablePiiLogging;
            CacheLogger = new MsalCacheLoggerWrapper(identityLogger, _loggerAdapterHelper.CorrelationId, _loggerAdapterHelper.ClientInformation);
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
                config?.EnablePiiLogging ?? false); ;
        }

        public void Log(EventLevel logLevel, string messageWithPii, string messageScrubbed)
        {
            LogEntry entry = _loggerAdapterHelper.Log(this, logLevel, messageWithPii, messageScrubbed);
            _identityLogger.Log(entry);
        }

        public bool IsLoggingEnabled(EventLevel eventLevel)
        {
            return _identityLogger.IsEnabled(eventLevel);
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
