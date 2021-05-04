// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using static Microsoft.Identity.Client.Kerberos.Win32.NativeMethods;

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
            int result = LsaDeregisterLogonProcess(this.handle);

            LsaThrowIfError(result);

            this.handle = IntPtr.Zero;

            return true;
        }
    }
}
