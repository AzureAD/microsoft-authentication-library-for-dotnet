// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CommonCache.Test.Common
{
    /// <summary>
    ///     Represents information about a running process.
    /// </summary>
    public interface IProcessRunningInfo : IDisposable
    {
        /// <summary>
        ///     Gets a value indicating the process exit code.
        /// </summary>
        int ExitCode { get; }

        /// <summary>
        ///     Gets a value indicating whether the process has exited.
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        ///     Gets the process id.
        /// </summary>
        int Id { get; }

        /// <summary>
        ///     Event that is risen when a process exits.
        /// </summary>
        event EventHandler Exited;
    }
}
