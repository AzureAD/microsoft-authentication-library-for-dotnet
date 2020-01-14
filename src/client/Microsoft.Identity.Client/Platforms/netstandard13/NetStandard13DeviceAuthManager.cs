// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    internal class NetStandard13DeviceAuthManager : IDeviceAuthManager
    {
        public bool CanHandleDeviceAuthChallenge { get { return false; } }

        public Task<string> CreateDeviceAuthChallengeResponseAsync(IDictionary<string, string> challengeData)
        {
            throw new NotImplementedException();
        }
    }
}
