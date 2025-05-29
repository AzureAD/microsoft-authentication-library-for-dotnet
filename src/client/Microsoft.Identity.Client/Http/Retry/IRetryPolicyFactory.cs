// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Microsoft.Identity.Client.Internal.Constants;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal interface IRetryPolicyFactory
    {
        IRetryPolicy GetRetryPolicy(RequestType requestType);
    }
}
