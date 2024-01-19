// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal static class LinuxNativeMethods
    {
        public const int RootUserId = 0;

        /// <summary>
        /// Get the real user ID of the calling process.
        /// </summary>
        /// <returns>the real user ID of the calling process</returns>
        [DllImport("libc")]
        public static extern int getuid();
    }
}
