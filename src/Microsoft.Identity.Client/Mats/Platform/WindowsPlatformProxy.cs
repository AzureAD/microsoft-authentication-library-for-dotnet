// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Platform
{
    internal class WindowsPlatformProxy : IPlatformProxy
    {
        public string GetDeviceNetworkState()
        {
            // TODO: 
            return string.Empty;
        }

        public string GetDpti()
        {
            // TODO: 
            return string.Empty;
        }

        public string GetOsPlatform()
        {
            return OsPlatformUtils.AsString(OsPlatform.Win32);
        }

        public int GetOsPlatformCode()
        {
            return OsPlatformUtils.AsInt(OsPlatform.Win32);
        }
    }
}
