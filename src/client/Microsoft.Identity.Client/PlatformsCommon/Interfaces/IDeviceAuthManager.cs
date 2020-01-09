// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IDeviceAuthManager
    {
        bool CanHandleDeviceAuthChallenge { get; }
        Task<string> CreateDeviceAuthChallengeResponseAsync(IDictionary<string, string> challengeData);
    }
}
