// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Forms;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{

    internal class Win32Window(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
