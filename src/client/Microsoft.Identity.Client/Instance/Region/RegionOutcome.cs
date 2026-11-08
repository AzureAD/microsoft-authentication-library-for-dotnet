// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Region
{
    /// <summary>
    /// Indicates where the region information came from. 
    /// </summary>
    public enum RegionOutcome
    {
        /// <summary>
        /// Indicates that no region outcome was recorded.
        /// </summary>
        None = 0,

        /// <summary>
        /// Region autodetect requested and was successful
        /// </summary>
        AutodetectSuccess = 4,

        /// <summary>
        /// Region autodetect requested but failed. Fallback to global
        /// </summary>
        FallbackToGlobal = 5,

        /// <summary>
        /// Region provided by the user and used without auto-detection.
        /// </summary>
        UserProvided = 6
    }
}
