// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
#if !(iOS || MAC || ANDROID)
    internal class LsaBufferSafeHandle : SafeHandle
    {
        public LsaBufferSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            var result = NativeMethods.LsaFreeReturnBuffer(this.handle);

            NativeMethods.LsaThrowIfError(result);

            this.handle = IntPtr.Zero;

            return true;
        }
    }
#endif
}
