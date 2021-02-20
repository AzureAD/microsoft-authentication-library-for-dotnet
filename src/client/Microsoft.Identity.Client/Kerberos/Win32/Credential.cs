// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

namespace Microsoft.Identity.Client.Kerberos.Win32
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
        /// <returns>Creted new <see cref="Credential"/> object.</returns>
        public static Credential Current()
        {
            return new CurrentCredential();
        }

        private class CurrentCredential : Credential
        {
            internal unsafe override CredentialHandle Structify()
            {
                return new CredentialHandle((void*)0);
            }
        }
    }
}