// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Data used to validate cache items for different authentication schemes.
    /// </summary>
    public class MsalCacheValidationData
    {
        /// <summary>
        /// Gets the persisted parameters addded to the cache items.
        /// </summary>
        public IDictionary<string, string> PersistedCacheParameters { get; internal set; }

        /// <summary>
        /// The cancellation token used to cancel cache validation operations.
        /// </summary>
        public CancellationToken cancellationToken { get; internal set; }
    }
}
