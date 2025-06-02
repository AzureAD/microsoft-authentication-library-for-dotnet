// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Http.Retry
{
    internal interface IRetryPolicyFactory
    {
        public enum RequestType
        {
            STS,
            ManagedIdentityDefault,
            Imds
        }

        IRetryPolicy GetRetryPolicy(RequestType requestType);
    }
}
