// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

//Will be replaced by logger package when available

namespace Microsoft.IdentityModel.Logging.Abstractions
{
    /// <summary>
    /// Defines the structure of a log entry.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Defines the <see cref="EventLevel"/>.
        /// </summary>
        public EventLevel EventLevel { get; set; }

        /// <summary>
        /// Message to be logged.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// A unique identifier for a request that can help with diagnostics across components.
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
