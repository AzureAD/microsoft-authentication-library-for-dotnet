// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents log level in MSAL.
    /// For details, see <see href="https://aka.ms/msal-net-logging">MSAL logging</see>.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Includes logs of important health metrics to help with diagnostics of MSAL operations.
        /// </summary>
        Always = -1,

        /// <summary>
        /// Includes logs when something has gone wrong and an error was generated. Used for debugging and identifying problems.
        /// </summary>
        Error = 0,

        /// <summary>
        /// Includes logs in scenarios when there hasn't necessarily been an error or failure, but are intended for diagnostics and pinpointing problems.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Default. Includes logs of general events intended for informational purposes, not necessarily intended for debugging.
        /// </summary>
        Info = 2,

        /// <summary>
        /// Includes logs of the full details of library behavior.
        /// </summary>
        Verbose = 3
    }
}
