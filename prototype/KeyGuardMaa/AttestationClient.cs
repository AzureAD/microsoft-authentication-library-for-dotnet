using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace KeyGuard.Attestation
{
    /// <summary>
    /// Managed facade for <c>AttestationClientLib.dll</c>.  Holds initialization state,
    /// does ref-count hygiene on <see cref="SafeNCryptKeyHandle"/>, and returns a JWT.
    /// </summary>
    public sealed class AttestationClient : IDisposable
    {
        private bool _initialized;

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
        public bool TryAttest(string endpoint,
                              SafeNCryptKeyHandle keyHandle,
                              out string? jwt,
                              string clientId = "kg-sample-client")
        {
            jwt = null;
            IntPtr buf = IntPtr.Zero;

            bool addRef = false;
            keyHandle.DangerousAddRef(ref addRef);

            try
            {
                int rc = NativeMethods.AttestKeyGuardImportKey(
                             endpoint,
                             authToken: null,
                             clientPayload: null,
                             keyHandle,
                             out buf,
                             clientId);

                if (rc == 0 && buf != IntPtr.Zero)
                    jwt = Marshal.PtrToStringAnsi(buf);

                return rc == 0;
            }
            finally
            {
                if (buf != IntPtr.Zero) NativeMethods.FreeAttestationToken(buf);
                if (addRef) keyHandle.DangerousRelease();
            }
        }

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
