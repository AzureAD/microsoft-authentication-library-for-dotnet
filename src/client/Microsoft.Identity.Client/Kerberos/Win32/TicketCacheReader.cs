// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
#pragma warning disable 618 // This workaround required for Native Win32 API call
    /// <summary>
    /// Helper class to check Kerberos Ticket in user's Ticket Cache.
    /// </summary>
    public class TicketCacheReader : IDisposable
    {
#if !(iOS || MAC || ANDROID)
        private readonly string _spn;
        private readonly SspiSecurityContext _context;
        private bool _disposedValue;
#endif

        /// <summary>
        /// Creates a <see cref="TicketCacheReader"/> object to read a Kerberos Ticket from Ticket Cache.
        /// </summary>
        /// <param name="spn">Service principal name of ticket to read out from Ticket Cache.</param>
        /// <param name="logonId">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <param name="package">The name of the LSA authentication package that will be interacted with.</param>
        public TicketCacheReader(string spn, long logonId = 0, string package = "Kerberos")
        {
#if (iOS || MAC || ANDROID)
            throw new NotSupportedException("Ticket Cache interface is not supported for this OS platform.");
#else
            if (!KerberosSupplementalTicketManager.IsWindows())
            {
                throw new NotSupportedException("Ticket Cache interface is not supported for this OS platform.");
            }

            this._spn = spn;
            this._context = new SspiSecurityContext(Credential.Current(), package, logonId);
#endif
        }


        /// <summary>
        /// Read out a Kerberos Ticket.
        /// </summary>
        /// <returns>Byte stream of Kereros Ticket if exists. Null otherwise.</returns>
        /// <remarks>
        /// Can throws <see cref="Win32Exception"/> if any error occurs while interfacing with Ticket Cache.
        /// </remarks>
        public byte[] RequestToken()
        {
#if (iOS || MAC || ANDROID)
            throw new NotSupportedException("Ticket Cache interface is not supported for this OS platform.");
#else
            var status = this._context.InitializeSecurityContext(this._spn, out byte[] clientRequest);

            if (status == ContextStatus.Error)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return clientRequest;
#endif
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
#if !(iOS || MAC || ANDROID)
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._context.Dispose();
                }

                this._disposedValue = true;
            }
#endif
        }

        /// <summary>
        /// Clean up all data members used for interaction with Ticket Cache.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

#pragma warning restore 618
}
