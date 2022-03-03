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
    public class ExternalToken
    {
        public string[] scopes { get; set; } // (or resource?) 

        public string correlationId { get; set; }

        public string tenantId { get; set; }

        public CancellationToken cancellationToken { get; set; }

        public DateTimeOffset Expiry { get; set; } // Mandatory 

        public DateTimeOffset? RefreshIn { get; set; } // Optional. If not provided compute Expiry-DateTimeOffset.Now()

        public string RawAccessToken { get; set; } // mandatory

        public string RawRefreshToken { get; set; } // optional
    }
}
