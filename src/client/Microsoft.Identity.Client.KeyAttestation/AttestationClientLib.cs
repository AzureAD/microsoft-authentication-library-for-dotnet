// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.KeyAttestation
{
    internal static class AttestationClientLib
    {
        internal enum LogLevel { Error, Warn, Info, Debug }

        internal delegate void LogFunc(
            IntPtr ctx, string tag, LogLevel lvl, string func, int line, string msg);

        [StructLayout(LayoutKind.Sequential)]
        internal struct AttestationLogInfo
        {
            public LogFunc Log;
            public IntPtr Ctx;
        }

        [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl,
                   CharSet = CharSet.Ansi)]
        internal static extern int InitAttestationLib(ref AttestationLogInfo info);

        [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl,
                   CharSet = CharSet.Ansi)]
        internal static extern int AttestKeyGuardImportKey(
            string endpoint,
            string authToken,
            string clientPayload,
            SafeNCryptKeyHandle keyHandle,
            out IntPtr token,
            string clientId);

        [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeAttestationToken(IntPtr token);

        [DllImport("AttestationClientLib.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void UninitAttestationLib();
    }
}
