// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Region;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains the result of region when MSAL region discovery is used, 
    /// published as part of AuthenticationResultMetadata.
    /// <see cref="AuthenticationResultMetadata"/> for additional metadata 
    /// information of the authentication result.
    /// </summary>
    public class RegionDetails
    {
        /// <summary>
        /// Constructor for RegionDetails
        /// </summary>
        /// <param name="regionOutcome"></param>
        /// <param name="regionUsed"></param>
        /// <param name="autoDetectionError "></param>
        public RegionDetails(
            RegionOutcome regionOutcome,
            string regionUsed,
            string autoDetectionError)
        {
            RegionOutcome = regionOutcome;
            RegionUsed = regionUsed;
            AutoDetectionError = autoDetectionError;
        }

        /// <summary>
        /// Region Outcome based on MSAL region detection
        /// </summary>
        public RegionOutcome RegionOutcome { get; }

        /// <summary>
        /// Region used to construct /token endpoint to contact ESTS.
        /// </summary>
        public string RegionUsed { get; }

        /// <summary>
        /// Error details when region auto detect fails
        /// </summary>
        public string AutoDetectionError  { get; }

    }
}
