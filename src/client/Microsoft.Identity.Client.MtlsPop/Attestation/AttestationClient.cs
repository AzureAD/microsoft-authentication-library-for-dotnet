﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>
    /// Managed façade for <c>AttestationClientLib.dll</c>.  Holds initialization state,
    /// does ref-count hygiene on <see cref="SafeNCryptKeyHandle"/>, and returns a JWT.
    /// </summary>
    internal sealed class AttestationClient : IDisposable
    {
        private bool _initialized;

        /// <summary>
        /// AttestationClient constructor. Pro-actively verifies the native DLL.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public AttestationClient()
        {
            /* step 0 ── ensure the resolver probes all valid locations
               (env override → app base → System32/SysWOW64 → PATH) */
            NativeDllResolver.EnsureLoaded();

            /* step 1 ── optional proactive verification (non-fatal)
               Keep the probe for diagnostics, but do NOT throw here; if the DLL
               is truly unavailable/mismatched, InitAttestationLib will fail. */
            string dllError = NativeDiagnostics.ProbeNativeDll();
            // intentionally not throwing on dllError to avoid path-specific false negatives

            /* step 2 ── load & initialize (logger is required by native lib) */
            var info = new AttestationClientLib.AttestationLogInfo
            {
                Log = AttestationLogger.ConsoleLogger, // minimal rooted delegate; works on netstandard2.0 & net8.0
                Ctx = IntPtr.Zero
            };

            _initialized = AttestationClientLib.InitAttestationLib(ref info) == 0;
            if (!_initialized)
                throw new InvalidOperationException("Failed to initialize AttestationClientLib.");
        }

        /// <summary>
        /// Calls the native <c>AttestKeyGuardImportKey</c> and returns a structured result.
        /// </summary>
        public AttestationResult Attest(string endpoint,
                                        SafeNCryptKeyHandle keyHandle,
                                        string clientId)
        {
            if (!_initialized)
                return new(AttestationStatus.NotInitialized, null, -1,
                           "Native library not initialized.");

            IntPtr buf = IntPtr.Zero;
            bool addRef = false;

            try
            {
                keyHandle.DangerousAddRef(ref addRef);

                int rc = AttestationClientLib.AttestKeyGuardImportKey(
                             endpoint, null, null, keyHandle, out buf, clientId);

                if (rc != 0)
                    return new(AttestationStatus.NativeError, null, rc, null);

                if (buf == IntPtr.Zero)
                    return new(AttestationStatus.TokenEmpty, null, 0,
                               "rc==0 but token buffer was null.");

                string jwt = Marshal.PtrToStringAnsi(buf)!;
                return new(AttestationStatus.Success, jwt, 0, null);
            }
            catch (DllNotFoundException ex)
            {
                return new(AttestationStatus.Exception, null, -1,
                    $"Native DLL not found: {ex.Message}");
            }
            catch (BadImageFormatException ex)
            {
                return new(AttestationStatus.Exception, null, -1,
                    $"Architecture mismatch (x86/x64) or corrupted DLL: {ex.Message}");
            }
            catch (SEHException ex)
            {
                return new(AttestationStatus.Exception, null, -1,
                    $"Native library raised SEHException: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new(AttestationStatus.Exception, null, -1, ex.Message);
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    AttestationClientLib.FreeAttestationToken(buf);
                if (addRef)
                    keyHandle.DangerousRelease();
            }
        }

        /// <summary>
        /// Disposes the client, releasing any resources and un-initializing the native library.
        /// </summary>
        public void Dispose()
        {
            if (_initialized)
            {
                AttestationClientLib.UninitAttestationLib();
                _initialized = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
