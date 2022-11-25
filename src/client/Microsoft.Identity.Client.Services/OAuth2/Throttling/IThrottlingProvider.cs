// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal interface IThrottlingProvider
    {
        void TryThrottle(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams);

        void RecordException(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams, 
            MsalServiceException ex);

        /// <summary>
        /// For test purposes, so that the static state can be reset
        /// </summary>
        void ResetCache();
    }
}
