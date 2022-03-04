// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Token result from external token provider
    /// </summary>
    public class ExternalTokenResult
    {
        /// <summary>
        /// Correlationn Id to track request
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Tenant Id for client application
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Cancellation token for async operation
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Expiration of token
        /// </summary>
        public long ExpiresInSeconds { get; set; } // Mandatory 

        /// <summary>
        /// When the token should be refreshed proactivly. (Optional)
        /// If not provided computed as Expiry-DateTimeOffset.Now()
        /// </summary>
        public long? RefreshInSeconds { get; set; } 

        /// <summary>
        /// Access token
        /// </summary>
        public string RawAccessToken { get; set; } // mandatory
    }
}
