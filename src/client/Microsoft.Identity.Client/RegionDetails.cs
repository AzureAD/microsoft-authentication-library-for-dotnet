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
    /// <remarks>
    /// Constructor for RegionDetails
    /// </remarks>
    /// <param name="regionOutcome"></param>
    /// <param name="regionUsed"></param>
    /// <param name="autoDetectionError "></param>
    public class RegionDetails(
        RegionOutcome regionOutcome,
        string regionUsed,
        string autoDetectionError)
    {

        /// <summary>
        /// Region Outcome based on MSAL region detection
        /// </summary>
        public RegionOutcome RegionOutcome { get; } = regionOutcome;

        /// <summary>
        /// Region used to construct /token endpoint to contact ESTS.
        /// </summary>
        public string RegionUsed { get; } = regionUsed;

        /// <summary>
        /// Error details when region auto detect fails
        /// </summary>
        public string AutoDetectionError { get; } = autoDetectionError;

    }
}
