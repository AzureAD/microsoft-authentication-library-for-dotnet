// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger.LogScrubber
{
    internal class PiiLogEntry
    {
        public EventLevel Level { get; }

        public LogArgument[] LogArgements { get; }

        public string LogFormat { get;  }

        public PiiLogEntry(string logFormat, EventLevel level, params LogArgument[] logArgements)
        {
            LogFormat = logFormat;
            LogArgements = logArgements;
            Level = level;
        }
    }
}
