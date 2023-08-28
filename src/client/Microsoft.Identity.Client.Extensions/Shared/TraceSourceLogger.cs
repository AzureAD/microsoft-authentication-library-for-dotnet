// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// 
    /// </summary>
    public class TraceSourceLogger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceSource"></param>
        public TraceSourceLogger(TraceSource traceSource)
        {
            Source = traceSource;
        }

        /// <summary>
        /// 
        /// </summary>
        public TraceSource Source { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void LogInformation(string message)
        {
            Source.TraceEvent(TraceEventType.Information, /*id*/ 0, FormatLogMessage(message));
        }

        /// <summary>
        /// 
        /// </summary>
        public void LogError(string message)
        {
            Source.TraceEvent(TraceEventType.Error, /*id*/ 0, FormatLogMessage(message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void LogWarning(string message)
        {
            Source.TraceEvent(TraceEventType.Warning, /*id*/ 0, FormatLogMessage(message));
        }

        private static string FormatLogMessage(string message)
        {
            return $"[MSAL.Extension][{DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)}] {message}";
        }
    }
}
