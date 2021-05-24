// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    internal class LsaBufferSafeHandle : SafeHandle
    {
        public LsaBufferSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            var result = NativeMethods.LsaFreeReturnBuffer(handle);

            NativeMethods.LsaThrowIfError(result);

            handle = IntPtr.Zero;
            return true;
        }
    }
}
