// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker.SplashScreen
{
    internal class Win32Window : IWin32Window
    {

        public Win32Window(IntPtr handle)
        {
            Handle = handle;
        }
        public IntPtr Handle { get; }

    }
}
