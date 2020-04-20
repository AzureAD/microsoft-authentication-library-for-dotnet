// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    internal static class WindowsNativeMethods
    {
        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

    }
}
