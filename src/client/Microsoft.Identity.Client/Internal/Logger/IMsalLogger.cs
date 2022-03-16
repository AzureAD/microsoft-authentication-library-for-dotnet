// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Logger interface for MSAL logging operations
    /// </summary>
    public interface IMsalLogger
    {
        /// <summary>
        /// Indicates whether or not PII loggign si enabled
        /// </summary>
        bool PiiLoggingEnabled { get; }

        /// <summary>
        /// Log messages into MSAL
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="messageWithPii"></param>
        /// <param name="messageScrubbed"></param>
        void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed);

        /// <summary>
        /// For expensive logging messsages (e.g. when the log message evaluates a variable), 
        /// it is better to check the log level ahead of time so as not to evaluate the expensive message and then discard it.
        /// </summary>
        bool IsLoggingEnabled(LogLevel logLevel);
    }
}
