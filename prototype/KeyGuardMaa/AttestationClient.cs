// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using static KeyGuard.Attestation.AttestationErrors;

namespace KeyGuard.Attestation
{
    /// <summary>
    /// Managed facade for <c>AttestationClientLib.dll</c>.  Holds initialization state,
    /// does ref-count hygiene on <see cref="SafeNCryptKeyHandle"/>, and returns a JWT.
    /// </summary>
    public sealed class AttestationClient : IDisposable
    {
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttestationClient"/> class.
        /// Ensures the native library is loaded and initializes the attestation library.
        /// Throws an <see cref="InvalidOperationException"/> if initialization fails.
        /// </summary>
        public AttestationClient()
        {
            NativeDllResolver.EnsureLoaded();                            // step 0

            var info = new NativeMethods.AttestationLogInfo
            {
                Log = AttestationLogger.ConsoleLogger,
                Ctx = IntPtr.Zero
            };

            _initialized = NativeMethods.InitAttestationLib(ref info) == 0;
            if (!_initialized)
                throw new InvalidOperationException("Failed to initialize AttestationClientLib.");
        }

        /// <summary>
        /// Calls the native <c>AttestKeyGuardImportKey</c> and—on success—returns the JWT.
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
                               "rc==0 but token buffer null.");

                string jwt = Marshal.PtrToStringAnsi(buf)!;
                return new(AttestationStatus.Success, jwt, 0, null);
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
        /// Disposes the <see cref="AttestationClient"/> instance, releasing any resources
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
