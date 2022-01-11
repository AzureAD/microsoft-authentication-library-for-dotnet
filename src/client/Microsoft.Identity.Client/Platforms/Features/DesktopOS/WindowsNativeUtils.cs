// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Utils.Windows
{
    /// <summary>
    /// Public Windows native methods
    /// </summary>
    public static class WindowsNativeUtils
    {
        /// <summary>
        /// Tests whether the current user is a member of the Administrator's group.
        /// </summary>
        /// <returns>True if the current user is an Admin; false, otherwise.</returns>
        public static bool IsElevatedUser() => IsUserAnAdmin();

        /// <summary>
        /// Registers security and sets the security values for the process.
        /// </summary>
        /// <remarks>
        /// Workaround to enable WAM Account Picker in an elevated process.
        /// </remarks>
        public static void InitializeProcessSecurity()
        {
            int result = InitializeProcessSecurityInternal();

            if (result != 0)
            {
                throw new MsalClientException(MsalError.InitializeProcessSecurityError, MsalErrorMessage.InitializeProcessSecurityError($"0x{result:x}"));
            }
        }

        internal static int InitializeProcessSecurityInternal()
        {
            int result = CoInitializeSecurity(
               IntPtr.Zero, -1, IntPtr.Zero,
               IntPtr.Zero, RpcAuthnLevel.None,
               RpcImpLevel.Impersonate, IntPtr.Zero,
               EoAuthnCap.None, IntPtr.Zero);

            return result;
        }

        [DllImport("shell32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        [DllImport("ole32.dll")]
        private static extern int CoInitializeSecurity(
            IntPtr pVoid, int cAuthSvc, IntPtr asAuthSvc,
            IntPtr pReserved1, RpcAuthnLevel level,
            RpcImpLevel impers, IntPtr pAuthList,
            EoAuthnCap dwCapabilities, IntPtr pReserved3);

        private enum RpcAuthnLevel
        {
            Default = 0,
            None = 1,
            Connect = 2,
            Call = 3,
            Pkt = 4,
            PktIntegrity = 5,
            PktPrivacy = 6
        }

        private enum RpcImpLevel
        {
            Default = 0,
            Anonymous = 1,
            Identify = 2,
            Impersonate = 3,
            Delegate = 4
        }

        private enum EoAuthnCap
        {
            None = 0x00,
            MutualAuth = 0x01,
            StaticCloaking = 0x20,
            DynamicCloaking = 0x40,
            AnyAuthority = 0x80,
            MakeFullSIC = 0x100,
            Default = 0x800,
            SecureRefs = 0x02,
            AccessControl = 0x04,
            AppID = 0x08,
            Dynamic = 0x10,
            RequireFullSIC = 0x200,
            AutoImpersonate = 0x400,
            NoCustomMarshal = 0x2000,
            DisableAAA = 0x1000
        }
    }
}
