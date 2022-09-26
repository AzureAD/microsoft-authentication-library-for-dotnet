// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
#if !XAMARINMAC20
    //This class is used to wrap the functionality of the configured IIdentityLogger to add additional MSAL client information when logging messages.
    internal class IdentityLogger : IIdentityLogger
    {
        private readonly IIdentityLogger _identityLogger;
        private readonly string _correlationId;
        private readonly string _clientInformation;
        private readonly bool _piiLoggingEnabled;

        internal IdentityLogger(IIdentityLogger identityLogger, Guid correlationId, string clientName, string clientVersion, bool enablePiiLogging)
        {
            _identityLogger = identityLogger;
            _correlationId = correlationId.Equals(Guid.Empty)
                ? string.Empty
                : " - " + correlationId;
            _clientInformation = LoggerHelper.GetClientInfo(clientName, clientVersion);
            _piiLoggingEnabled = enablePiiLogging;
        }

        public bool IsEnabled(EventLogLevel eventLevel)
        {
            return _identityLogger.IsEnabled(eventLevel);
        }

        public void Log(LogEntry entry)
        {
            entry.Message = LoggerHelper.FormatLogMessage(
                                            entry.Message, 
                                            _piiLoggingEnabled, 
                                            !string.IsNullOrEmpty(entry.CorrelationId) ? entry.CorrelationId : _correlationId, 
                                            _clientInformation);

            _identityLogger.Log(entry);
        }
    }
#endif
}
