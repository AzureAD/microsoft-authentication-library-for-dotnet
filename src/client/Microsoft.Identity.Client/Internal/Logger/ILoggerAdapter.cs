// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Logger.LogScrubber;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    interface ILoggerAdapter
    {
        bool IsPiiEnabled { get; }
        void Log(LogEntry entry);
        void LogWithPii(PiiLogEntry piiEntry);
    }
}
