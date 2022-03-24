// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Will be replaced by logger package when available

namespace Microsoft.IdentityModel.Logging.Abstractions
{
    /// <summary>
    /// Interface for Logging.
    /// </summary>
    public interface IIdentityLogger
    {
        /// <summary>
        /// 
        /// </summary>
        bool IsPiiEnabled { get; }

        /// <summary>
        /// Checks to see if logging is enabled at given <paramref name="eventLevel"/>.
        /// </summary>
        /// <param name="eventLevel">Log level of an Event.</param>
        bool IsEnabled(EventLevel eventLevel);

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="entry">Defines a structured message to be logged at the provided <see cref="LogEntry.EventLevel"/>.</param>
        void Log(LogEntry entry);
    }
}
