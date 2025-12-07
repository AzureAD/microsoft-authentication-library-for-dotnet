// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class RetryPolicyFactory : IRetryPolicyFactory
    {
        public virtual IRetryPolicy GetRetryPolicy(RequestType requestType)
        {
            switch (requestType)
            {
                case RequestType.STS:
                case RequestType.ManagedIdentityDefault:
                    return new DefaultRetryPolicy(requestType);
                case RequestType.Imds:
                    return new ImdsRetryPolicy();
                case RequestType.RegionDiscovery:
                    return new RegionDiscoveryRetryPolicy();
                case RequestType.CsrMetadataProbe:
                    return new CsrMetadataProbeRetryPolicy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, "Unknown request type.");
            }
        }
    }
}
