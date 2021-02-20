// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    /// <summary>
    /// Extension of a wrapper class for operating system handles.
    /// </summary>
    internal class CredentialHandle : SafeHandle
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cred">Credential handle to initialize.</param>
        public unsafe CredentialHandle(void* cred)
            : base(new IntPtr(cred), true)
        {
        }

        /// <summary>
        /// Checks the current contained handle is valid or not.
        /// </summary>
        public override bool IsInvalid => this.handle == IntPtr.Zero;

        /// <summary>
        /// Release contained internal resource object.
        /// </summary>
        /// <returns>True always.</returns>
        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
}