// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static Microsoft.Identity.Client.Kerberos.Win32.NativeMethods;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    internal class LsaTokenSafeHandle : SafeHandle
    {
        public LsaTokenSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public bool Impersonating { get; private set; }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            this.Revert();

            if (!CloseHandle(this.handle))
            {
                var error = Marshal.GetLastWin32Error();

                throw new Win32Exception(error);
            }

            return true;
        }

        public void Impersonate()
        {
            this.Revert();

            if (!ImpersonateLoggedOnUser(this))
            {
                var error = Marshal.GetLastWin32Error();

                throw new Win32Exception(error);
            }

            this.Impersonating = true;
        }

        private void Revert()
        {
            if (!this.Impersonating)
            {
                return;
            }

            if (!RevertToSelf())
            {
                var error = Marshal.GetLastWin32Error();

                throw new Win32Exception(error);
            }

            this.Impersonating = false;
        }
    }
}