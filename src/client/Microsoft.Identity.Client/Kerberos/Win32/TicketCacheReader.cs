// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    /// <summary>
    /// Helper class to check Kerberos Ticket in user's Ticket Cache.
    /// </summary>
    public class TicketCacheReader : IDisposable
    {
        private readonly string spn;
        private readonly SspiSecurityContext context;
        private bool disposedValue;

        public TicketCacheReader(string spn, string package = "Kerberos")
        {
            this.spn = spn;

            this.context = new SspiSecurityContext(Credential.Current(), package);
        }

        public byte[] RequestToken()
        {
            var status = this.context.InitializeSecurityContext(this.spn, out byte[] clientRequest);

            if (status == ContextStatus.Error)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return clientRequest;
        }

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

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
