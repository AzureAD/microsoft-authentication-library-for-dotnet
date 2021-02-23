// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

using System.Globalization;

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// Utility class to manage Kerberos Claims to get Kerberos Service Ticket.
    /// </summary>
    internal class KerberosClaimManager
    {
        internal const string KerberosClaimType = "xms_as_rep";
        internal const string IdTokenAsRepTemplate = @"{{""id_token"": {{ ""xms_as_rep"":{{""essential"":{0},""value"":""{1}""}} }} }}";
        internal const string AccessTokenAsRepTemplate = @"{{""access_token"": {{ ""xms_as_rep"":{{""essential"":{0},""value"":""{1}""}} }} }}";

        /// <summary>
        /// Get <see cref="KerberosSupplementalTicket"/> object from received Json Web Token.
        /// </summary>
        /// <param name="idToken">ID token with Json Web Token format.</param>
        /// <returns>A <see cref="KerberosSupplementalTicket"/> object if exists and parsed correctly. Null, otherwise.</returns>
        internal static KerberosSupplementalTicket Parse(string idToken)
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

            KerberosSupplementalTicket ticket = KerberosSupplementalTicket.FromJson(kerberosAsRep);
            if (ticket != null)
            {
                KerberosSupplementalTicket.SaveToCache(ticket);
            }

            return ticket;
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
