// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Class for Kerberos tickets that are included as claims and used as a supplemental token in an OAuth/OIDC
    /// protocol response.
    /// </summary>
    public class KerberosSupplementalTicket
    {
        /// <summary>
        /// Get or Sets the client key used to encrypt the client portion of the ticket.
        /// This is optional. This will be null if KeyType is null.
        /// This MUST be protected in the protocol response.
        /// </summary>
        [JsonProperty("clientKey")]
        public string ClientKey { get; set; }

        /// <summary>
        /// Get or Sets the client key type.This is optional.This will be null if ClientKey is null.
        /// </summary>
        [JsonProperty("keyType")]
        public KerberosKeyTypes KeyType { get; set; }

        /// <summary>
        /// Get or Sets the Base64 encoded KERB_MESSAGE_BUFFER
        /// </summary>
        [JsonProperty("messageBuffer")]
        public string KerberosMessageBuffer { get; set; }

        /// <summary>
        /// Get or Sets the error message that server encountered when creating a ticket granting ticket.
        /// </summary>
        [JsonProperty("error")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Get or Sets the Kerberos realm/domain name.
        /// </summary>
        [JsonProperty("realm")]
        public string Realm { get; set; }

        /// <summary>
        /// Get or Sets the target service principal name (SPN).
        /// </summary>
        [JsonProperty("sn")]
        public string ServicePrincipalName { get; set; }

        /// <summary>
        /// Get or Sets the client name. Depending on the ticket, this can be either a UserPrincipalName or SamAccountName.
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
            return $"[ Realm: {Realm}, sp: {ServicePrincipalName}, cn: {ClientName}, KeyType: {KeyType} ]";
        }
    }
}
