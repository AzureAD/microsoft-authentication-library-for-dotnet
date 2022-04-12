// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Logger.LogScrubber;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class IdentityLoggerAdapter : ILoggerAdapter
    {
        private IIdentityLogger _identityLogger;
        private ILogScrubber _scrubber;

        public bool IsPiiEnabled { get; }

        public IdentityLoggerAdapter(IIdentityLogger identityLogger, bool piiEnabled, ILogScrubber scrubber)
        {
            _identityLogger = identityLogger;
            _scrubber = scrubber;
            IsPiiEnabled = piiEnabled;
        }

        public void Log(LogEntry entry)
        {
            _identityLogger.Log(entry);
        }

        public bool IsEnabled(EventLevel eventLevel)
        {
            return _identityLogger.IsEnabled(eventLevel);
        }

        public void LogWithPii(PiiLogEntry piiEntry)
        {
            _scrubber.ScrubLogArguments(piiEntry.LogArgements);

            var LogArgementsAsStrings = piiEntry.LogArgements.Select(x => x.ToString());

            _identityLogger.Log(new LogEntry()
            {
                Message = string.Format(piiEntry.LogFormat, LogArgementsAsStrings),
                EventLevel = piiEntry.Level
            });
        }
    }
}
