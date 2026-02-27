// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Managed façade for <c>AttestationClientLib.dll</c>. Holds initialization state,
    /// does ref-count hygiene on <see cref="SafeNCryptKeyHandle"/>, and returns a JWT with expiry information.
    /// </summary>
    internal sealed class AttestationClient : IDisposable
    {
        private bool _initialized;

        /// <summary>
        /// AttestationClient constructor. Relies on the default OS loader to locate the native DLL.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public AttestationClient()
        {
            string dllError = NativeDiagnostics.ProbeNativeDll();
            // intentionally not throwing on dllError

            // Load & initialize (logger is required by native lib)
            var info = new AttestationClientLib.AttestationLogInfo
            {
                Log = AttestationLogger.ConsoleLogger,
                Ctx = IntPtr.Zero
            };

            _initialized = AttestationClientLib.InitAttestationLib(ref info) == 0;
            if (!_initialized)
                throw new InvalidOperationException("Failed to initialize AttestationClientLib.");
        }

        /// <summary>
        /// Calls the native <c>AttestKeyGuardImportKey</c> and returns a structured result with expiry information.
        /// </summary>
        public AttestationResult Attest(string endpoint,
                                        SafeNCryptKeyHandle keyHandle,
                                        string clientId)
        {
            if (!_initialized)
                return new(AttestationStatus.NotInitialized, null, null, -1,
                    "Native library not initialized.");

            IntPtr buf = IntPtr.Zero;
            bool addRef = false;

            try
            {
                keyHandle.DangerousAddRef(ref addRef);

                int rc = AttestationClientLib.AttestKeyGuardImportKey(
                    endpoint, null, null, keyHandle, out buf, clientId);

                if (rc != 0)
                    return new(AttestationStatus.NativeError, null, null, rc, null);

                if (buf == IntPtr.Zero)
                    return new(AttestationStatus.TokenEmpty, null, null, 0,
                        "rc==0 but token buffer was null.");

                string jwt = Marshal.PtrToStringAnsi(buf)!;

                // Extract expiry from JWT payload
                JwtClaimExtractor.TryExtractExpirationClaim(jwt, out DateTimeOffset expiresOn);

                var token = new AttestationToken(jwt, expiresOn);
                return new(AttestationStatus.Success, token, jwt, 0, null);
            }
            catch (DllNotFoundException ex)
            {
                return new(AttestationStatus.Exception, null, null, -1,
                    $"Native DLL not found: {ex.Message}");
            }
            catch (BadImageFormatException ex)
            {
                return new(AttestationStatus.Exception, null, null, -1,
                    $"Architecture mismatch (x86/x64) or corrupted DLL: {ex.Message}");
            }
            catch (SEHException ex)
            {
                return new(AttestationStatus.Exception, null, null, -1,
                    $"Native library raised SEHException: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new(AttestationStatus.Exception, null, null, -1, ex.Message);
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
