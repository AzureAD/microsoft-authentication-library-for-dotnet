// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Text;
using static Microsoft.Identity.Client.Kerberos.Win32.NativeMethods;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    /// <summary>
    /// Provides a layer to interact with the LSA functions used to create logon sessions and manipulate the ticket caches.
    /// </summary>
    public class LsaInterop : IDisposable
    {
        private const string KerberosPackageName = "Kerberos";
        private const string NegotiatePackageName = "Negotiate";

        private readonly LsaSafeHandle lsaHandle;
        private readonly int selectedAuthPackage;
        private readonly int negotiateAuthPackage;

        private bool disposedValue;

        /*
         * Windows creates a new ticket cache for primary NT tokens. This allows callers to create a dedicated cache for whatever they're doing
         * that way the cache operations like purge or import don't polute the current users cache.
         *
         * To make this work we need to create a new NT token, which is only done during logon. We don't actually want Windows to validate the credentials
         * so we tell it to treat the logon as `NewCredentials` which means Windows will just use those credentials as SSO credentials only.
         *
         * From there a new cache is created and any operations against the "current cache" such as SSPI ISC calls will hit this new cache.
         * We then let callers import tickets into that cache using the krb-cred structure.
         *
         * When done the call to dispose will
         * 1. Revert the impersonation context
         * 2. Close the NT token handle
         * 3. Close the Lsa Handle
         *
         * This destroys the cache and closes the logon session.
         *
         * For any operation that require native allocation and PtrToStructure copies we try and use the CryptoPool mechanism, which checks out a shared
         * pool of memory to create a working for the current operation. On dispose it zeros the memory and returns it to the pool.
         */

        internal unsafe LsaInterop(LsaSafeHandle lsaHandle, string packageName = KerberosPackageName)
        {
            this.lsaHandle = lsaHandle;

            var kerberosPackageName = new LSA_STRING
            {
                Buffer = packageName,
                Length = (ushort)packageName.Length,
                MaximumLength = (ushort)packageName.Length
            };

            var result = LsaLookupAuthenticationPackage(this.lsaHandle, ref kerberosPackageName, out this.selectedAuthPackage);
            LsaThrowIfError(result);

            var negotiatePackageName = new LSA_STRING
            {
                Buffer = NegotiatePackageName,
                Length = (ushort)NegotiatePackageName.Length,
                MaximumLength = (ushort)NegotiatePackageName.Length
            };

            result = LsaLookupAuthenticationPackage(this.lsaHandle, ref negotiatePackageName, out this.negotiateAuthPackage);
            LsaThrowIfError(result);
        }

        /// <summary>
        /// Create a new instance of the interop as a standard unprivileged caller.
        /// </summary>
        /// <param name="package">The name of the LSA authentication package that will be interacted with.</param>
        /// <returns>Returns an instance of the <see cref="LsaInterop"/> class.</returns>
        public static LsaInterop Connect(string package = KerberosPackageName)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                package = KerberosPackageName;
            }

            var result = LsaConnectUntrusted(out LsaSafeHandle lsaHandle);

            LsaThrowIfError(result);

            return new LsaInterop(lsaHandle, package);
        }

        /// <summary>
        /// Import a kerberos ticket containing one or more tickets into the current user ticket cache.
        /// </summary>
        /// <param name="ticketBytes">The ticket to import into the cache.</param>
        /// <param name="luid">The Logon Id of the user owning the ticket cache. The default of 0 represents the currently logged on user.</param>
        public unsafe void ImportCredential(byte[] ticketBytes, long luid = 0)
        {
            if (ticketBytes is null)
            {
                throw new ArgumentNullException(nameof(ticketBytes));
            }

            var ticketRequest = new KERB_SUBMIT_TKT_REQUEST
            {
                MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbSubmitTicketMessage,
                KerbCredSize = ticketBytes.Length,
                KerbCredOffset = Marshal.SizeOf(typeof(KERB_SUBMIT_TKT_REQUEST)),
                LogonId = luid
            };

            var bufferSize = ticketRequest.KerbCredOffset + ticketBytes.Length;
            IntPtr pBuffer = Marshal.AllocHGlobal(bufferSize);

            Marshal.StructureToPtr(ticketRequest, (IntPtr)pBuffer, false);
            Marshal.Copy(ticketBytes, 0, pBuffer + ticketRequest.KerbCredOffset, ticketBytes.Length);
            this.LsaCallAuthenticationPackage(pBuffer.ToPointer(), bufferSize);
        }

        /// <summary>
        /// Call Auth package to cache given Kerberos ticket.
        /// </summary>
        /// <param name="pBuffer">Pointer to Kerberos Ticket to cache.</param>
        /// <param name="bufferSize">Length of Kerberos Ticket data.</param>

        private unsafe void LsaCallAuthenticationPackage(void* pBuffer, int bufferSize)
        {
            LsaBufferSafeHandle returnBuffer = null;

            try
            {
                var result = NativeMethods.LsaCallAuthenticationPackage(
                    this.lsaHandle,
                    this.selectedAuthPackage,
                    pBuffer,
                    bufferSize,
                    out returnBuffer,
                    out int returnBufferLength,
                    out int protocolStatus
                );

                LsaThrowIfError(result);
                LsaThrowIfError(protocolStatus);
            }
            finally
            {
                returnBuffer?.Dispose();
            }
        }

        /// <summary>
        /// Dispose all interment members.
        /// </summary>
        /// <param name="disposing">True if Dispose() called by the user. False, otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.lsaHandle.Dispose();
                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Deletes current object.
        /// </summary>
        ~LsaInterop()
        {
            this.Dispose(disposing: false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
