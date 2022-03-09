// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

using System;

namespace Microsoft.Identity.Client.Region
{
    /// <summary>
    /// Indicates where the region information came from. 
    /// </summary>
    public enum RegionOutcome
    {
        /// <summary>
        /// Indicates that the API .WithAzureRegion() was not used
        /// </summary>
        None = 0,

        /// <summary>
        /// Region provided by the user, matches auto detected region
        /// </summary>
        UserProvidedValid = 1,

        /// <summary>
        /// Region provided by the user, auto detection cannot be done
        /// </summary>
        UserProvidedAutodetectionFailed = 2,

        /// <summary>
        /// Region provided by the user, does not match auto detected region
        /// </summary>
        UserProvidedInvalid = 3,

        /// <summary>
        /// Region autodetect requested and was successful
        /// </summary>
        AutodetectSuccess = 4,

        /// <summary>
        /// Region autodetect requested but failed. Fallback to global
        /// </summary>
        FallbackToGlobal = 5
    }
}
