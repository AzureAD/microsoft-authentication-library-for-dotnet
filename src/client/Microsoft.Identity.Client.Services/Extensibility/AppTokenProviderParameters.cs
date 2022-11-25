// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// The authentication parameters provided to the app token provider callback.
    /// </summary>
    public class AppTokenProviderParameters
    {
        /// <summary>
        /// Specifies which scopes to request.
        /// </summary>
        public IEnumerable<string> Scopes { get; internal set; }

        /// <summary>
        /// Correlation id of the authentication request.
        /// </summary>
        public string CorrelationId { get; internal set; }

        /// <summary>
        /// A string with one or multiple claims.
        /// </summary>
        public string Claims { get; internal set; }

        /// <summary>
        /// Tenant id of the 
        /// </summary>
        public string TenantId { get; internal set; }

        /// <summary>
        /// Used to cancel the authentication attempt made by the token provider
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }
    }
}
