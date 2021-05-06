// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
#if !(iOS || MAC || ANDROID)
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
#endif
}
