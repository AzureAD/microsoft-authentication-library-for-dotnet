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
        public IEnumerable<string> scopes { get; }

        public string correlationId { get; }

        public string Claims { get; }

        public string tenantId { get; }

        public CancellationToken cancellationToken { get; }
    }
}
