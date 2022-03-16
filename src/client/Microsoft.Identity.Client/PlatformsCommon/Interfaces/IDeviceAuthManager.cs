// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http.Headers;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IDeviceAuthManager
    {
        bool TryCreateDeviceAuthChallengeResponseAsync(HttpResponseHeaders headers, Uri endpointUri, out string responseHeader);
    }
}
