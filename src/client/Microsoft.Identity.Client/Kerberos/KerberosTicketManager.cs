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
    /// Helper class to manage Kerberos Ticket features.
    /// </summary>
    public class KerberosTicketManager
    {
        internal const string KerberosClaimType = "xms_as_rep";
        internal const string IdTokenAsRepTemplate = @"{{""id_token"": {{ ""xms_as_rep"":{{""essential"":{0},""value"":""{1}""}} }} }}";
        internal const string AccessTokenAsRepTemplate = @"{{""access_token"": {{ ""xms_as_rep"":{{""essential"":{0},""value"":""{1}""}} }} }}";

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
        /// <param name="errorMessage">Output parameter. Error message if error occurs within the function.</param>
        /// <param name="luid">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <returns>True if Ticket save to Ticket Cache successfully. False, otherwise.</returns>
        public static bool SaveToCache(KerberosSupplementalTicket ticket, out string errorMessage, long luid = 0)
        {
            errorMessage = null;

#if SUPPORT_KERBEROS
            if (ticket == null || string.IsNullOrEmpty(ticket.KerberosMessageBuffer))
            {
                errorMessage = "Kerberos Ticket information is not valid";
                return false;
            }

            try
            {
                using (var cache = Win32.TicketCacheWriter.Connect())
                {
                    byte[] krbCred = Convert.FromBase64String(ticket.KerberosMessageBuffer);
                    cache.ImportCredential(krbCred, luid);
                    return true;
                }
            }
            catch (Win32Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Checks a Kerberos Service Ticket associated with given service principal name exists
        /// in current user's Ticket Cache.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to find associated Kerberos Ticket.</param>
        /// <param name="errorMessage">Output parameter. Error message if error occurs within the function.</param>
        /// <param name="luid">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <returns>True if Kerberos Ticket exists. False, otherwise.</returns>
        public static bool IsTKerberosTicketExistsInCache(string servicePrincipalName, out string errorMessage, long luid = 0)
        {
            return (GetKerberosTicketFromCache(servicePrincipalName, out errorMessage, luid) != null);
        }

        /// <summary>
        /// Reads a Kerberos Service Ticket associated with given service principal name from
        /// current user's Ticket Cache.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to find associated Kerberos Ticket.</param>
        /// <param name="errorMessage">Output parameter. Error message if error occurs within the function.</param>
        /// <param name="luid">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        /// <returns>Byte stream of searched Kerberos Ticket information if exists. Null, otherwise.</returns>
        public static byte[] GetKerberosTicketFromCache(string servicePrincipalName, out string errorMessage, long luid = 0)
        {
            errorMessage = null;

#if SUPPORT_KERBEROS
            try
            {
                using (var reader = new Win32.TicketCacheReader(servicePrincipalName))
                {
                    return reader.RequestToken();
                }
            }
            catch (Win32Exception ex)
            {
                errorMessage = ex.Message;
                return null;
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
                        "false",
                        requestParams.RequestContext.ServiceBundle.Config.KerberosServicePrincipalName);
                }
                else
                {
                    kerberosClaim = string.Format(
                        CultureInfo.InvariantCulture,
                        AccessTokenAsRepTemplate,
                        "false",
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
