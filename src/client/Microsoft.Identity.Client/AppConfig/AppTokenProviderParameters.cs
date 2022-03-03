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
    /// 
    /// </summary>
    public class AppTokenProviderParameters
    {
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> Scopes { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public string CorrelationId { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public string Claims { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public string TenantId { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }
    }
}
