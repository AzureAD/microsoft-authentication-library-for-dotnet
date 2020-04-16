// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal interface IThrottlingManager
    {
        void RecordException(RequestContext requestContext, MsalServiceException ex);
        void ThrottleIfNeeded(RequestContext requestContext);

        /// <summary>
        /// For test purposes, so that the static state can be reset
        /// </summary>
        void Reset();
    }
}
