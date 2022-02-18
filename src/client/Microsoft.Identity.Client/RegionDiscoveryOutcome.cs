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
    public class RegionDiscoveryOutcome
    {
        /// <summary>
        /// Constructor for RegionDiscoveryOutcome
        /// </summary>
        /// <param name="regionOutcome"></param>
        /// <param name="regionUsed"></param>
        /// <param name="exception"></param>
        internal RegionDiscoveryOutcome(
            RegionOutcome regionOutcome,
            string regionUsed,
            string exception)
        {
            RegionOutcome = regionOutcome;
            RegionUsed = regionUsed;
            Exception = exception;
        }

        /// <summary>
        /// Region Outcome based on MSAL region detection
        /// </summary>
        internal RegionOutcome RegionOutcome { get; }

        /// <summary>
        /// Region returned to the user
        /// </summary>
        internal string RegionUsed { get; }

        /// <summary>
        /// Error details when region auto detect fails
        /// </summary>
        internal string Exception { get; }

    }
}
