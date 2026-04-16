// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByUserFederatedIdentityCredentialParameters : IAcquireTokenParameters
    {
        public string Username { get; set; }
        public string Assertion { get; set; }
        public bool? SendX5C { get; set; }
        public bool ForceRefresh { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
        }
    }
}
