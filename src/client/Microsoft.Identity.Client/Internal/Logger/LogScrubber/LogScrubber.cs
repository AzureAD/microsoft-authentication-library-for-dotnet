// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.IdentityModel.Logging.Abstractions
{
    /// <summary>
    /// Default implementation of the <see cref="ILogScrubber"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="LogArgument"/> instances with <see cref="DataClassification"/> category lower than <see cref="MinimumDataClassificationCategory"/>
    /// will be replaced with '(Scrubbed, <see cref="MinimumDataClassificationCategory"/>)'.
    /// </remarks>
    public class LogScrubber : ILogScrubber
    {
        /// <summary>
        /// Gets or sets the minimum <see cref="DataClassification"/> category.
        /// </summary>
        public DataClassification MinimumDataClassificationCategory { get; set; } = DataClassification.SystemMetadata;

        /// <inheritdoc/>
        public void ScrubLogArguments(params LogArgument[] args)
        {
            foreach (var arg in args)
            {
                if (arg?.DataClassificationCategory < MinimumDataClassificationCategory)
                {
                    if (arg?.Argument == null)
                        continue;

                    arg.Argument = $"(Scrubbed, {arg.DataClassificationCategory})";
                }
            }
        }
    }
}
