// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Used for platforms that do not implement PKeyAuth.
    /// </summary>
    internal class NullDeviceAuthManager : IDeviceAuthManager
    {
        public bool TryCreateDeviceAuthChallengeResponseAsync(HttpResponseHeaders headers, Uri endpointUri, out string responseHeader)
        {
            if (!DeviceAuthHelper.IsDeviceAuthChallenge(headers))
            {
                responseHeader = string.Empty;
                return false;
            }

            //Bypassing challenge
            responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(headers);
            return true;
        }
    }
}
