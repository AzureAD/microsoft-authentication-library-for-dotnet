// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IDeviceAuthManager
    {
        bool TryCreateDeviceAuthChallengeResponse(HttpResponseHeaders headers, Uri endpointUri, out string responseHeader);
    }
}
