// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>Ensures <c>AttestationClientLib.dll</c> is resolved from the exe folder.</summary>
    internal static class NativeDllResolver
    {
        private const string NativeDll = "AttestationClientLib.dll";
        private static IntPtr s_module;

        static NativeDllResolver()
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, NativeDll);
            
            if (!File.Exists(fullPath))
            {
                return;
            }

            s_module = WindowsDllLoader.Load(fullPath);
        }

        /// <summary>
        /// Touch this method from your startup code to trigger the static ctor.
        /// </summary>
        internal static void EnsureLoaded() { }
    }
}
