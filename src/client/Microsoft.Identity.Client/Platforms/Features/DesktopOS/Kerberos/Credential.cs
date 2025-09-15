﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    /// <summary>
    /// Previously authenticated logon data used by a security principal to establish its own identity, 
    /// such as a password, or a Kerberos protocol ticket.
    /// </summary>
    public abstract class Credential
    {
        internal abstract CredentialHandle Structify();

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
            internal override unsafe CredentialHandle Structify()
            {
                return new CredentialHandle((void*)0);
            }
        }
    }
}
