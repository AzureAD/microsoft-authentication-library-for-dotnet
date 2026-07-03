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
        /// Indicates that no region outcome was recorded. This includes requests where .WithAzureRegion()
        /// was not used, and requests where an explicit region was used without auto-discovery.
        /// </summary>
        None = 0,

        /// <summary>
        /// Region provided by the user, matches auto detected region.
        /// </summary>
        [Obsolete("MSAL no longer performs auto-discovery for explicitly configured regions. This value is no longer returned.", false)]
        UserProvidedValid = 1,

        /// <summary>
        /// Region provided by the user, auto detection cannot be done.
        /// </summary>
        [Obsolete("MSAL no longer performs auto-discovery for explicitly configured regions. This value is no longer returned.", false)]
        UserProvidedAutodetectionFailed = 2,

        /// <summary>
        /// Region provided by the user, does not match auto detected region.
        /// </summary>
        [Obsolete("MSAL no longer performs auto-discovery for explicitly configured regions. This value is no longer returned.", false)]
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
