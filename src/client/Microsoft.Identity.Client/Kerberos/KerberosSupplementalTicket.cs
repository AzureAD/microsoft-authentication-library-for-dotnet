// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Json;

using System;

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Class for Kerberos tickets that are included as claims and used as a supplemental token in an OAuth/OIDC
    /// protocol response.
    /// </summary>
    public class KerberosSupplementalTicket
    {
        /// <summary>
        /// The client key used to encrypt the client portion of the ticket. This is optional. This will be null if
        /// KeyType is null. This MUST be protected in the protocol response.
        /// </summary>
        [JsonProperty("clientKey")]
        public string ClientKey { get; set; }

        /// <summary>
        /// The client key type.This is optional.This will be null if ClientKey is null.
        /// </summary>
        [JsonProperty("keyType")]
        public KerberosKeyTypes KeyType { get; set; }

        /// <summary>
        /// Base64 encoded KERB_MESSAGE_BUFFER
        /// </summary>
        [JsonProperty("messageBuffer", Required = Required.Always)]
        public string KerberosMessageBuffer { get; set; }

        /// <summary>
        /// Contains the errors or failures that server encountered when creating a ticket granting ticket
        /// </summary>
        [JsonProperty("error")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The Kerberos realm/domain name.
        /// </summary>
        [JsonProperty("realm")]
        public string Realm { get; set; }

        /// <summary>
        /// The target service principal name (SPN).
        /// </summary>
        [JsonProperty("sn", Required = Required.Always)]
        public string ServicePrincipalName { get; set; }

        /// <summary>
        /// The client name. Depending on the ticket, this can be either a UserPrincipalName or SamAccountName.
        /// </summary>
        [JsonProperty("cn")]
        public string ClientName { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="KerberosSupplementalTicket"/> class.
        /// </summary>
        public KerberosSupplementalTicket()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="KerberosSupplementalTicket"/> class with error message.
        /// </summary>
        /// <param name="errorMessage">Error message to be set.</param>
        public KerberosSupplementalTicket(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[ Realm: {Realm}, ServicePrincipalName: {ServicePrincipalName}, ClientName: {ClientName}, KeyType: {KeyType} ]";
        }

        /// <summary>
        /// Creates a  <see cref="KerberosSupplementalTicket"/> object from Kerberos Ticket Claim response string
        /// parsed from Id Token.
        /// </summary>
        /// <param name="kerberosAsRep">Kerberos Ticket Claim response string parsed from id token.</param>
        /// <returns>A <see cref="KerberosSupplementalTicket"/> object created from given Kerberos Ticket Claim response string.
        /// Null, if error occurs.</returns>
        internal static KerberosSupplementalTicket FromJson(string kerberosAsRep)
        {
            if (string.IsNullOrEmpty(kerberosAsRep))
            {
                return null;
            }

            return (KerberosSupplementalTicket)JsonConvert.DeserializeObject(
                        kerberosAsRep,
                        typeof(KerberosSupplementalTicket));
        }

        /// <summary>
        /// Save current Kerberos Ticket to current user's Ticket Cache.
        /// </summary>
        internal static void SaveToCache(KerberosSupplementalTicket ticket)
        {
            if (ticket == null || ticket.KerberosMessageBuffer == null)
            {
                return;
            }

#if SUPPORT_KERBEROS
            using (var cache = Win32.LsaInterop.Connect())
            {
                byte[] krbCred = Convert.FromBase64String(ticket.KerberosMessageBuffer);
                cache.ImportCredential(krbCred);
            }
#endif
        }
    }
}
