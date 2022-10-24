// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Declares the type of container to use for Kerberos Ticket Claim.
    /// </summary>
    public enum KerberosTicketContainer
    {
        /// <summary>
        /// Use the Id token as the Kerberos Ticket container.
        /// (NOTE) MSAL will read out Kerberos Service Ticket from received id token, cache into current user's
        /// ticket cache, and return it as KerberosSupplementalTicket object in AuthenticationResult.
        /// </summary>
        IdToken = 0,

        /// <summary>
        /// Use the Access Token as the Kerberos Ticket container.
        /// (NOTE) MSAL will not read out Kerberos Service Ticket from received access token. Caller should handle
        /// received access token directly to use for next service request.
        /// </summary>
        AccessToken = 1
    }
}
