// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

using System;
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
        /// Creates a <see cref="KerberosSupplementalTicket"/> object from given JWT token string..
        /// </summary>
        /// <param name="jwtToken">JWT token string.</param>
        /// <returns>A <see cref="KerberosSupplementalTicket"/> object if exists and parsed correctly. Null, otherwise.</returns>
        public static KerberosSupplementalTicket FromToken(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken) || jwtToken.Length < 128)
            {
                // Token is empty or too short -ignore parsing.
                return null;
            }

            KerberosIdTokenParser jwt = KerberosIdTokenParser.Parse(jwtToken);
            if (jwt == null)
            {
                return null;
            }

            string kerberosAsRep = jwt.GetValueOrEmptyString(KerberosClaimType);
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
        /// <param name="ticket">Kerberos ticket object to save.</param>
        /// <param name="luid">The Logon Id of the user owning the ticket cache.
        /// The default of 0 represents the currently logged on user.</param>
        public static bool SaveToCache(KerberosSupplementalTicket ticket, long luid = 0)
        {
            if (ticket == null || ticket.KerberosMessageBuffer == null)
            {
                return false;
            }

#if SUPPORT_KERBEROS
            using (var cache = Win32.LsaInterop.Connect())
            {
                byte[] krbCred = Convert.FromBase64String(ticket.KerberosMessageBuffer);
                cache.ImportCredential(krbCred, luid);
                return true;
            }
#endif
            return false;
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
