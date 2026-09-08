// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Configures managed identity capability discovery.
    /// </summary>
    public sealed class ManagedIdentityCapabilitiesOptions
    {
        /// <summary>
        /// Gets or sets the total time allowed for uncached managed identity capability discovery.
        /// </summary>
        /// <remarks>
        /// The timeout covers discovery lock contention, IMDS probes and retries, fallback,
        /// compute metadata retrieval, and binding-strength detection. A <c>null</c> value
        /// preserves the existing cancellation ordering and waits without a discovery timeout.
        /// When a timeout is configured, an already-canceled caller token is observed before
        /// uncached discovery begins. Cancellation is cooperative, so non-interruptible platform
        /// work may finish before the timeout is observed.
        /// </remarks>
        public TimeSpan? CapabilityDiscoveryTimeout { get; set; }
    }
}
