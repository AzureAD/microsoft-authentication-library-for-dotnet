// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Platform
{
    internal interface IPlatformProxy
    {
        string GetDpti();
        string GetDeviceNetworkState();
        int GetOsPlatformCode();
        string GetOsPlatform();
    }
}
