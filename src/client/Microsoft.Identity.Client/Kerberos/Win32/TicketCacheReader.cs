// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
// This workaround required for Native Win32 API call
#pragma warning disable 618

    /// <summary>
    /// Helper class to check Kerberos Ticket in user's Ticket Cache.
    /// </summary>
    public class TicketCacheReader : IDisposable
    {
        private readonly string spn;
        private readonly SspiSecurityContext context;
        private bool disposedValue;

        /// <summary>
        /// Creates a <see cref="TicketCacheReader"/> object to read a Kerberos Ticket from Ticket Cache.
        /// </summary>
        /// <param name="spn">Service principal name of ticket to read out from Ticket Cache.</param>
        /// <param name="logonId">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <param name="package">The name of the LSA authentication package that will be interacted with.</param>
        public TicketCacheReader(string spn, long logonId = 0, string package = "Kerberos")
        {
            this.spn = spn;

            this.context = new SspiSecurityContext(Credential.Current(), package, logonId);
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
            var status = this.context.InitializeSecurityContext(this.spn, out byte[] clientRequest);

            if (status == ContextStatus.Error)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return clientRequest;
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.context.Dispose();
                }

                this.disposedValue = true;
            }
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
