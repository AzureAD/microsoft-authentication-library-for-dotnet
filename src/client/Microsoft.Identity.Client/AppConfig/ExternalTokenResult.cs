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
        /// Access token
        /// Mandatory
        /// </summary>
        public string RawAccessToken { get; set; }

        /// <summary>
        /// Tenant Id for client application
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Expiration of token
        /// Mandatory
        /// </summary>
        public long ExpiresInSeconds { get; set; } 

        /// <summary>
        /// When the token should be refreshed proactivly. (Optional)
        /// If not provided computed as Expiry-DateTimeOffset.Now()
        /// </summary>
        public long? RefreshInSeconds { get; set; } 
    }
}
