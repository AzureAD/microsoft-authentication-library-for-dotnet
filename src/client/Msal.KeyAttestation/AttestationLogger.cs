// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Msal.KeyAttestation
{
    internal static class AttestationLogger
    {
        /// <summary>
        /// Attestation Logger
        /// </summary>
        internal static readonly AttestationClientLib.LogFunc ConsoleLogger = (ctx, tag, lvl, func, line, msg) =>
        {
            try
            {
                string sTag = ToText(tag);
                string sFunc = ToText(func);
                string sMsg = ToText(msg);

                var lineText = $"[MtlsPop][{lvl}] {sTag} {sFunc}:{line}  {sMsg}";

                // Default: Trace (respects listeners; safe for all app types)
                Trace.WriteLine(lineText);
            }
            catch
            {
            }
        };

        // Converts either string or IntPtr (char*) to text. Works with any LogFunc variant.
        private static string ToText(object value)
        {
            if (value is IntPtr p && p != IntPtr.Zero)
            {
                try
                { return Marshal.PtrToStringAnsi(p) ?? string.Empty; }
                catch { return string.Empty; }
            }
            return value?.ToString() ?? string.Empty;
        }
    }
}
