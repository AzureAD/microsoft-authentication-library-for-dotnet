// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IDeviceAuthManager
    {
        bool TryCreateDeviceAuthChallengeResponseAsync(HttpResponse response, Uri endpointUri, out string responseHeader);
    }
}
