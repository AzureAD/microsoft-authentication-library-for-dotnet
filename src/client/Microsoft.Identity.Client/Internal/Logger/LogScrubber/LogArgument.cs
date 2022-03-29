// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.IdentityModel.Logging.Abstractions
{
    /// <summary>
    /// Class used to tag a log argument value with a <see cref="DataClassification"/> category.
    /// </summary>
    /// <remarks>
    /// Based on a data classification category of a log argument, Mise users may choose to alter the <see cref="Argument"/>
    /// to satisfy GDPR and other privacy requirements. Mise users may define <see cref="ILogScrubber"/> which operates on <see cref="Argument"/>
    /// based on its <see cref="DataClassificationCategory"/>.
    /// </remarks>
    public class LogArgument
    {
        /// <summary>
        /// Argument value.
        /// </summary>
        public string Argument { get; set; }

        /// <summary>
        /// Data classification category.
        /// </summary>
        public DataClassification DataClassificationCategory { get; set; }

        /// <summary>
        /// Creates a log argument.
        /// </summary>
        /// <param name="arg">Argument value.</param>
        /// <param name="dataClassificationCategory">data classification category</param>
        public LogArgument(string arg, DataClassification dataClassificationCategory)
        {
            Argument = arg;
            DataClassificationCategory = dataClassificationCategory;
        }

        /// <summary>
        /// Uses <see cref="Argument"/> value to override the base <see cref="ToString"/> method.
        /// </summary>
        /// <returns><see cref="Argument"/>.<see cref="ToString"/>, or <c>null</c> if <see cref="Argument"/> is <c>null</c>.</returns>
        public override string ToString()
        {
            return Argument ?? "null";
        }
    }
}
