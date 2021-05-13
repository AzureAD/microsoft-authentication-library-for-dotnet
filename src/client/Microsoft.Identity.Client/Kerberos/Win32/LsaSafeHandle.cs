// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    internal class LsaSafeHandle : SafeHandle
    {
        public LsaSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
#if !(iOS || MAC || ANDROID)
            int result = NativeMethods.LsaDeregisterLogonProcess(this.handle);

            NativeMethods.LsaThrowIfError(result);

            this.handle = IntPtr.Zero;
#endif
            return true;
        }
    }
}
