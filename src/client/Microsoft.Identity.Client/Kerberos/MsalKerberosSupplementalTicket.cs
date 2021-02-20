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
    public class MsalKerberosSupplementalTicket
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
        public MsalKerberosKeyTypes KeyType { get; set; }

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
        /// Creates a new instance of <see cref="MsalKerberosSupplementalTicket"/> class.
        /// </summary>
        public MsalKerberosSupplementalTicket()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="MsalKerberosSupplementalTicket"/> class with error message.
        /// </summary>
        /// <param name="errorMessage">Error message to be set.</param>
        public MsalKerberosSupplementalTicket(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Save current Kerberos Ticket content to cache.
        /// </summary>
        public static void SaveToCache(MsalKerberosSupplementalTicket ticket)
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

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[ Realm: {Realm}, ServicePrincipalName: {ServicePrincipalName}, ClientName: {ClientName}, KeyType: {KeyType} ]";
        }

        /// <summary>
        /// Instantiate this object from a JSON encoded JSON string.
        /// </summary>
        /// <param name="json">The JSON encoded JSON string.</param>
        /// <returns>The instantiated object.</returns>
        public static MsalKerberosSupplementalTicket FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return (MsalKerberosSupplementalTicket)JsonConvert.DeserializeObject(
                        json,
                        typeof(MsalKerberosSupplementalTicket));
        }
    }
}
