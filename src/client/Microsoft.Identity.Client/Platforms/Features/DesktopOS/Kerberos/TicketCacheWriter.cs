// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{

#pragma warning disable 618 // This workaround required for Native Win32 API call

    /// <summary>
    /// Provides a layer to interact with the LSA functions used to create logon sessions and manipulate the ticket caches.
    /// </summary>
    public class TicketCacheWriter : IDisposable
    {
        private const string _kerberosPackageName = "Kerberos";
        private const string _negotiatePackageName = "Negotiate";

        private readonly LsaSafeHandle _lsaHandle;
        private readonly int _selectedAuthPackage;
        private readonly int _negotiateAuthPackage;
        private bool _disposedValue;

        /*
         * Windows creates a new ticket cache for primary NT tokens. This allows callers to create a dedicated cache for whatever they're doing
         * that way the cache operations like purge or import don't pollute the current users cache.
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

        internal unsafe TicketCacheWriter(LsaSafeHandle lsaHandle, string packageName = _kerberosPackageName)
        {
            _lsaHandle = lsaHandle;

            var kerberosPackageName = new NativeMethods.LSA_STRING
            {
                Buffer = packageName,
                Length = (ushort)packageName.Length,
                MaximumLength = (ushort)packageName.Length
            };

            var result = NativeMethods.LsaLookupAuthenticationPackage(_lsaHandle, ref kerberosPackageName, out _selectedAuthPackage);
            NativeMethods.LsaThrowIfError(result);

            var negotiatePackageName = new NativeMethods.LSA_STRING
            {
                Buffer = _negotiatePackageName,
                Length = (ushort)_negotiatePackageName.Length,
                MaximumLength = (ushort)_negotiatePackageName.Length
            };

            result = NativeMethods.LsaLookupAuthenticationPackage(_lsaHandle, ref negotiatePackageName, out _negotiateAuthPackage);
            NativeMethods.LsaThrowIfError(result);
        }

        /// <summary>
        /// Create a new instance of the interop as a standard unprivileged caller.
        /// </summary>
        /// <param name="package">The name of the LSA authentication package that will be interacted with.</param>
        /// <returns>Returns an instance of the <see cref="TicketCacheWriter"/> class.</returns>
        public static TicketCacheWriter Connect(string package = _kerberosPackageName)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                package = _kerberosPackageName;
            }

            var result = NativeMethods.LsaConnectUntrusted(out LsaSafeHandle _lsaHandle);

            NativeMethods.LsaThrowIfError(result);

            return new TicketCacheWriter(_lsaHandle, package);
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

            var ticketRequest = new NativeMethods.KERB_SUBMIT_TKT_REQUEST
            {
                MessageType = NativeMethods.KERB_PROTOCOL_MESSAGE_TYPE.KerbSubmitTicketMessage,
                KerbCredSize = ticketBytes.Length,
                KerbCredOffset = Marshal.SizeOf(typeof(NativeMethods.KERB_SUBMIT_TKT_REQUEST)),
                LogonId = luid
            };

            var bufferSize = ticketRequest.KerbCredOffset + ticketBytes.Length;
            IntPtr pBuffer = Marshal.AllocHGlobal(bufferSize);

            Marshal.StructureToPtr(ticketRequest, pBuffer, false);
            Marshal.Copy(ticketBytes, 0, pBuffer + ticketRequest.KerbCredOffset, ticketBytes.Length);
            LsaCallAuthenticationPackage(pBuffer.ToPointer(), bufferSize);
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
                    _lsaHandle,
                    _selectedAuthPackage,
                    pBuffer,
                    bufferSize,
                    out returnBuffer,
                    out int _,
                    out int protocolStatus
                );

                NativeMethods.LsaThrowIfError(result);
                NativeMethods.LsaThrowIfError(protocolStatus);
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
            if (!_disposedValue)
            {
                _lsaHandle.Dispose();
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Deletes current object.
        /// </summary>
        ~TicketCacheWriter()
        {
            Dispose(disposing: false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

#pragma warning restore 618
}
