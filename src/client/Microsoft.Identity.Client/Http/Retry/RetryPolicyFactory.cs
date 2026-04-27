// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class RetryPolicyFactory : IRetryPolicyFactory
    {
        public virtual IRetryPolicy GetRetryPolicy(RequestType requestType)
        {
            return requestType switch
            {
                RequestType.STS or RequestType.ManagedIdentityDefault => new DefaultRetryPolicy(requestType),
                RequestType.ImdsProbe => new ImdsProbeRetryPolicy(),
                RequestType.Imds => new ImdsRetryPolicy(),
                RequestType.RegionDiscovery => new RegionDiscoveryRetryPolicy(),
                _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType, "Unknown request type."),
            };
        }
    }
}
