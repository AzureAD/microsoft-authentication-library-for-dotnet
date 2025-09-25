// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Test.Unit.Helpers
{
    internal class TestValidatedProbeEndpointFactory : IValidatedProbeEndpointFactory
    {
        public Uri GetValidatedEndpoint(ILoggerAdapter logger, string subPath, string queryParams)
        {
            return ImdsManagedIdentitySource.GetValidatedEndpoint(logger, subPath, queryParams, Microsoft.Identity.Test.Unit.ManagedIdentityTests.ManagedIdentityTests.ImdsEndpoint);
        }
    }
}
