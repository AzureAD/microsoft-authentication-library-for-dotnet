// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http.Retry;

namespace Microsoft.Identity.Test.Unit.Helpers
{
    internal class TestRetryPolicyFactory : IRetryPolicyFactory
    {
        public virtual IRetryPolicy GetRetryPolicy(RequestType requestType)
        {
            switch (requestType)
            {
                case RequestType.STS:
                case RequestType.ManagedIdentityDefault:
                    return new TestDefaultRetryPolicy(requestType);
                case RequestType.Imds:
                    return new TestImdsRetryPolicy();
                case RequestType.RegionDiscovery:
                    return new TestRegionDiscoveryRetryPolicy();
                case RequestType.CsrMetadataProbe:
                    return new TestCsrMetadataProbeRetryPolicy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, "Unknown request type.");
            }
        }
    }
}
