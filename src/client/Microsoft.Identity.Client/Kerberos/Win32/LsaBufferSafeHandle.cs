// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using static Microsoft.Identity.Client.Kerberos.Win32.NativeMethods;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    internal class LsaBufferSafeHandle : SafeHandle
    {
        public LsaBufferSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            var result = LsaFreeReturnBuffer(this.handle);

            LsaThrowIfError(result);

            this.handle = IntPtr.Zero;

            return true;
        }
    }
}
