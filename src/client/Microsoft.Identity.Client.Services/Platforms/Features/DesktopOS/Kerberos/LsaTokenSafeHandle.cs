// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    internal class LsaTokenSafeHandle : SafeHandle
    {
        public LsaTokenSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public bool Impersonating { get; private set; }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Revert();

            if (!NativeMethods.CloseHandle(handle))
            {
                var error = Marshal.GetLastWin32Error();

                throw new Win32Exception(error);
            }
            return true;
        }

        private void Revert()
        {
            if (!Impersonating)
            {
                return;
            }

            if (!NativeMethods.RevertToSelf())
            {
                var error = Marshal.GetLastWin32Error();

                throw new Win32Exception(error);
            }
            Impersonating = false;
        }
    }
}
