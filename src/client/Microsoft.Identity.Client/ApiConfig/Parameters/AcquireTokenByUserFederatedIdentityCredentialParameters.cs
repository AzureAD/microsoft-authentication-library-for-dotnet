// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByUserFederatedIdentityCredentialParameters : IAcquireTokenParameters
    {
        public string Username { get; set; }

        public Func<Task<string>> AssertionCallback { get; set; }

        public string TokenExchangeScope { get; set; }

        public bool? SendX5C { get; set; }

        public bool ForceRefresh { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
        }
    }
}
