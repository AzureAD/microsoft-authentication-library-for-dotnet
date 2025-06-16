// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using static KeyGuard.Attestation.AttestationErrors;

namespace KeyGuard.Attestation
{
    /// <summary>
    /// Managed façade for <c>AttestationClientLib.dll</c>.  Holds initialisation state,
    /// does ref-count hygiene on <see cref="SafeNCryptKeyHandle"/>, and returns a JWT.
    /// </summary>
    public sealed class AttestationClient : IDisposable
    {
        private bool _initialized;

        /// <summary>
        /// AttestationClient constructor.  Pro-actively verifies the native DLL,
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public AttestationClient()
        {
            /* step 0 ── pro-actively verify the native DLL */
            string? dllError = NativeDiagnostics.ProbeNativeDll();
            if (dllError is not null)
                throw new InvalidOperationException(dllError);

            /* step 1 ── load & initialise */
            NativeDllResolver.EnsureLoaded();

            var info = new NativeMethods.AttestationLogInfo
            {
                Log = AttestationLogger.ConsoleLogger,
                Ctx = IntPtr.Zero
            };

            _initialized = NativeMethods.InitAttestationLib(ref info) == 0;
            if (!_initialized)
                throw new InvalidOperationException("Failed to initialise AttestationClientLib.");
        }

        /// <summary>
        /// Calls the native <c>AttestKeyGuardImportKey</c> and returns a structured result.
        /// </summary>
        public AttestationResult Attest(string endpoint,
                                        SafeNCryptKeyHandle keyHandle,
                                        string clientId = "kg-sample-client")
        {
            if (!_initialized)
                return new(AttestationStatus.NotInitialized, null, -1,
                           "Native library not initialised.");

            IntPtr buf = IntPtr.Zero;
            bool addRef = false;

            try
            {
                keyHandle.DangerousAddRef(ref addRef);

                int rc = NativeMethods.AttestKeyGuardImportKey(
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
                    NativeMethods.FreeAttestationToken(buf);
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
                NativeMethods.UninitAttestationLib();
                _initialized = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
