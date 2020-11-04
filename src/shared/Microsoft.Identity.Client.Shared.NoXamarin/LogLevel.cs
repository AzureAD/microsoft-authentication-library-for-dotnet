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
        ///     Error Log level
        /// </summary>
        Error = 0,

        /// <summary>
        ///     Warning Log level
        /// </summary>
        Warning = 1,

        /// <summary>
        ///     Information Log level
        /// </summary>
        Info = 2,

        /// <summary>
        ///     Verbose Log level
        /// </summary>
        Verbose = 3
    }
}
