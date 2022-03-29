// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.IdentityModel.Logging.Abstractions
{
    /// <summary>
    /// Contract for a log scrubber
    /// </summary>
    public interface ILogScrubber
    {
        /// <summary>
        /// Alter <see cref="LogArgument.Argument"/> values based on their <see cref="LogArgument.DataClassificationCategory"/>.
        /// </summary>
        /// <param name="args">List of log arguments</param>
        void ScrubLogArguments(params LogArgument[] args);
    }
}
