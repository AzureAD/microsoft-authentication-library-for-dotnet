// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
#if !XAMARINMAC20
    //This class is used to wrap the functionality of the configured IIdentityLogger to add additional MSAL cleint information when logging messages.
    internal class MsalCacheLoggerWrapper : IIdentityLogger
    {
        private readonly IIdentityLogger _identityLogger;
        private readonly string _correlationId;
        private readonly string _clientInformation;

        internal MsalCacheLoggerWrapper(IIdentityLogger identityLogger, Guid correlationId, string clientName, string clientVersion)
        {
            _identityLogger = identityLogger;
            _correlationId = correlationId.Equals(Guid.Empty)
                ? string.Empty
                : " - " + correlationId;
            _clientInformation = LoggerAdapterHelper.GetClientInfo(clientName, clientVersion);
        }

        public static IIdentityLogger Create(
            Guid correlationId,
            ApplicationConfiguration config)
        {
            return new MsalCacheLoggerWrapper(
                config?.IdentityLogger,
                correlationId,
                config?.ClientName ?? string.Empty,
                config?.ClientVersion ?? string.Empty);
        }

        public bool IsEnabled(EventLogLevel eventLevel)
        {
            return _identityLogger.IsEnabled(eventLevel);
        }

        public void Log(LogEntry entry)
        {
            entry.Message = LoggerAdapterHelper.FormatLogMessage(string.Empty, entry.Message, false, _correlationId, _clientInformation);

            _identityLogger.Log(entry);
        }
    }
#endif
}
