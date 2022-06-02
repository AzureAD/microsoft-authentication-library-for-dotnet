// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
#if !XAMARINMAC20
    internal class MsalCacheLoggerWrapper : IIdentityLogger
    {
        private readonly IIdentityLogger _identityLogger;
        private readonly string _correlationId;
        private readonly string _clientInformation;

        internal MsalCacheLoggerWrapper(IIdentityLogger identityLogger, string correlationId, string clientInformation)
        {
            _identityLogger = identityLogger;
            _correlationId = correlationId;
            _clientInformation = clientInformation;
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
