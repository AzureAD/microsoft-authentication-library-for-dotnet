// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    ///     Level of the log messages.
    ///     For details see https://aka.ms/msal-net-logging
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        ///     HealthMetric Log Level
        /// </summary>
        HealthMetric = 0,

        /// <summary>
        ///     Error Log level
        /// </summary>
        Error = 1,

        /// <summary>
        ///     Warning Log level
        /// </summary>
        Warning = 2,

        /// <summary>
        ///     Information Log level
        /// </summary>
        Info = 3,

        /// <summary>
        ///     Verbose Log level
        /// </summary>
        Verbose = 4
    }
}
