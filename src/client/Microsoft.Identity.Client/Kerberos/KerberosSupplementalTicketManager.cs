// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Helper class to manage Kerberos Ticket Claims.
    /// </summary>
    public static class KerberosSupplementalTicketManager
    {
        private const int DefaultLogonId = 0;
        private const string KerberosClaimType = "xms_as_rep";
        private const string IdTokenAsRepTemplate = @"{{""id_token"": {{ ""xms_as_rep"":{{""essential"":""false"",""value"":""{0}""}} }} }}";
        private const string AccessTokenAsRepTemplate = @"{{""access_token"": {{ ""xms_as_rep"":{{""essential"":""false"",""value"":""{0}""}} }} }}";

        /// <summary>
        /// Creates a <see cref="KerberosSupplementalTicket"/> object from given ID token string..
        /// </summary>
        /// <param name="idToken">ID token string.</param>
        /// <returns>A <see cref="KerberosSupplementalTicket"/> object if exists and parsed correctly. Null, otherwise.</returns>
        public static KerberosSupplementalTicket FromIdToken(string idToken)
        {
            if (string.IsNullOrEmpty(idToken) || idToken.Length < 128)
            {
                // Token is empty or too short -ignore parsing.
                return null;
            }

            KerberosIdTokenParser jwt = KerberosIdTokenParser.Parse(idToken);
            if (jwt == null)
            {
                return null;
            }

            string kerberosAsRep = jwt.GetValueOrEmptyString(KerberosClaimType);
            if (string.IsNullOrEmpty(kerberosAsRep))
            {
                return null;
            }

            return (KerberosSupplementalTicket)JsonConvert.DeserializeObject(kerberosAsRep, typeof(KerberosSupplementalTicket));
        }

        /// <summary>
        /// Save current Kerberos Ticket to current user's Ticket Cache.
        /// </summary>
        /// <param name="ticket">Kerberos ticket object to save.</param>
        /// <remarks>Can throws <see cref="ArgumentException"/> when given ticket parameter is not a valid Kerberos Supplemental Ticket.
        /// Can throws <see cref="Win32Exception"/> if error occurs while saving ticket information into Ticket Cache.
        /// </remarks>
        public static void SaveToCache(KerberosSupplementalTicket ticket)
        {
            SaveToCache(ticket, DefaultLogonId);
        }

        /// <summary>
         /// Save current Kerberos Ticket to current user's Ticket Cache.
         /// </summary>
         /// <param name="ticket">Kerberos ticket object to save.</param>
         /// <param name="logonId">The Logon Id of the user owning the ticket cache.
         /// The default of 0 represents the currently logged on user.</param>
         /// <remarks>Can throws <see cref="ArgumentException"/> when given ticket parameter is not a valid Kerberos Supplemental Ticket.
         /// Can throws <see cref="Win32Exception"/> if error occurs while saving ticket information into Ticket Cache.
         /// </remarks>
        public static void SaveToCache(KerberosSupplementalTicket ticket, long logonId)
        {
#if SUPPORT_KERBEROS
            if (ticket == null || string.IsNullOrEmpty(ticket.KerberosMessageBuffer))
            {
                throw new ArgumentException("Kerberos Ticket information is not valid");
            }

            using (var cache = Win32.TicketCacheWriter.Connect())
            {
                byte[] krbCred = Convert.FromBase64String(ticket.KerberosMessageBuffer);
                cache.ImportCredential(krbCred, logonId);
            }
#endif
        }

        /// <summary>
        /// Checks a Kerberos Service Ticket associated with given service principal name exists
        /// in current user's Ticket Cache.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to find associated Kerberos Ticket.</param>
        /// <returns>True if Kerberos Ticket exists. False, otherwise.</returns>
        /// <returns>True if Ticket save to Ticket Cache successfully. False, otherwise.</returns>
        /// <remarks>
        /// Can throws <see cref="Win32Exception"/> if error occurs while searching ticket information from Ticket Cache.
        /// </remarks>
        public static bool IsTKerberosTicketExistsInCache(string servicePrincipalName)
        {
            return IsTKerberosTicketExistsInCache(servicePrincipalName, DefaultLogonId);
        }

        /// <summary>
        /// Checks a Kerberos Service Ticket associated with given service principal name exists
        /// in current user's Ticket Cache.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to find associated Kerberos Ticket.</param>
        /// <param name="logonId">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <returns>True if Kerberos Ticket exists. False, otherwise.</returns>
        /// <returns>True if Ticket save to Ticket Cache successfully. False, otherwise.</returns>
        /// <remarks>
        /// Can throws <see cref="Win32Exception"/> if error occurs while searching ticket information from Ticket Cache.
        /// </remarks>
        public static bool IsTKerberosTicketExistsInCache(string servicePrincipalName, long logonId)
        {
            return (GetKerberosTicketFromCache(servicePrincipalName, logonId) != null);
        }

        /// <summary>
        /// Reads a Kerberos Service Ticket associated with given service principal name from
        /// current user's Ticket Cache.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to find associated Kerberos Ticket.</param>
        /// <returns>Byte stream of searched Kerberos Ticket information if exists. Null, otherwise.</returns>
        /// <remarks>
        /// Can throws <see cref="Win32Exception"/> if error occurs while searching ticket information from Ticket Cache.
        /// </remarks>
        public static byte[] GetKerberosTicketFromCache(string servicePrincipalName)
        {
            return GetKerberosTicketFromCache(servicePrincipalName, DefaultLogonId);
        }

        /// <summary>
        /// Reads a Kerberos Service Ticket associated with given service principal name from
        /// current user's Ticket Cache.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to find associated Kerberos Ticket.</param>
        /// <param name="logonId">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <returns>Byte stream of searched Kerberos Ticket information if exists. Null, otherwise.</returns>
        /// <remarks>
        /// Can throws <see cref="Win32Exception"/> if error occurs while searching ticket information from Ticket Cache.
        /// </remarks>
        public static byte[] GetKerberosTicketFromCache(string servicePrincipalName, long logonId)
        {
#if SUPPORT_KERBEROS
            using (var reader = new Win32.TicketCacheReader(servicePrincipalName, logonId))
            {
                return reader.RequestToken();
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Gets the KRB-CRED Kerberos Ticket information as byte stream.
        /// </summary>
        /// <param name="ticket">Kerberos ticket object to save.</param>
        /// <returns>Byte stream representaion of KRB-CRED Kerberos Ticket if it contains valid ticket information.
        /// Null, otherwise.</returns>
        public static byte[] GetKrbCred(KerberosSupplementalTicket ticket)
        {
            if (!string.IsNullOrEmpty(ticket.KerberosMessageBuffer))
            {
                return Convert.FromBase64String(ticket.KerberosMessageBuffer);
            }

            return null;
        }

        /// <summary>
        /// Add Claims to body parameter for POST request.
        /// </summary>
        /// <param name="oAuth2Client"><see cref="OAuth2Client"/> object for Token request.</param>
        /// <param name="requestParams"><see cref="AuthenticationRequestParameters"/> containing request parameters.</param>
        internal static void AddKerberosTicketClaim(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParams)
        {
            if (!string.IsNullOrEmpty(requestParams.RequestContext.ServiceBundle.Config.KerberosServicePrincipalName))
            {
                string kerberosClaim;
                if (requestParams.RequestContext.ServiceBundle.Config.TicketContainer == KerberosTicketContainer.IdToken)
                {
                    kerberosClaim = string.Format(
                        CultureInfo.InvariantCulture,
                        IdTokenAsRepTemplate,
                        requestParams.RequestContext.ServiceBundle.Config.KerberosServicePrincipalName);
                }
                else
                {
                    kerberosClaim = string.Format(
                        CultureInfo.InvariantCulture,
                        AccessTokenAsRepTemplate,
                        requestParams.RequestContext.ServiceBundle.Config.KerberosServicePrincipalName);
                }

                if (string.IsNullOrEmpty(requestParams.ClaimsAndClientCapabilities))
                {
                    oAuth2Client.AddBodyParameter(OAuth2Parameter.Claims, kerberosClaim);
                }
                else
                {
                    JObject existingClaims = JObject.Parse(requestParams.ClaimsAndClientCapabilities);
                    JObject mergedClaims
                        = ClaimsHelper.MergeClaimsIntoCapabilityJson(kerberosClaim, existingClaims);

                    oAuth2Client.AddBodyParameter(OAuth2Parameter.Claims, mergedClaims.ToString(Formatting.None));
                }
            }
            else
            {
                oAuth2Client.AddBodyParameter(OAuth2Parameter.Claims, requestParams.ClaimsAndClientCapabilities);
            }
        }
    }
}
