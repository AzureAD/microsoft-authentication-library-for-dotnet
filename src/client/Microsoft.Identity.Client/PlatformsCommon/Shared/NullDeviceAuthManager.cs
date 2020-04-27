// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
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
