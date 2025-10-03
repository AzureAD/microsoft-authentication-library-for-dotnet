// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    /// <summary>
    /// Previously authenticated logon data used by a security principal to establish its own identity, 
    /// such as a password, or a Kerberos protocol ticket.
    /// </summary>
    [Obsolete]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Credential
    {

        /// <summary>
        /// Create a new <see cref="Credential"/> object.
        /// </summary>
        /// <returns>Newly created <see cref="Credential"/> object.</returns>
        public static Credential Current()
        {
            return new CurrentCredential();
        }

        private class CurrentCredential : Credential
        {
            
        }
    }
}
