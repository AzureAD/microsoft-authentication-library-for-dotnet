// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client
{
    internal static class TokenResponseHelper
    {
        internal const string NullPreferredUsernameDisplayLabel = "Missing from the token response";

        public static string GetTenantId(IdToken idToken, AuthenticationRequestParameters requestParams)
        {
            // If the input authority was tenanted, use that tenant over the IdToken.Tenant
            // otherwise, this will result in cache misses
            return Authority.CreateAuthorityWithTenant(
                requestParams.Authority.AuthorityInfo,
                idToken?.TenantId).TenantId;
        }

        public static string GetPreferredUsernameFromIdToken(bool isAdfsAuthority, IdToken idToken)
        {
            // The preferred_username value cannot be null or empty in order to comply with the ADAL/MSAL Unified cache schema.
            // It will be set to "Missing from the token response"
            if (idToken == null)
            {
                return NullPreferredUsernameDisplayLabel;
            }

            if (string.IsNullOrWhiteSpace(idToken.PreferredUsername))
            {
                if (isAdfsAuthority)
                {
                    //The direct to ADFS scenario does not return preferred_username in the id token so it needs to be set to the UPN
                    return !string.IsNullOrEmpty(idToken.Upn)
                        ? idToken.Upn
                        : NullPreferredUsernameDisplayLabel;
                }
                return NullPreferredUsernameDisplayLabel;
            }

            return idToken.PreferredUsername;
        }

        public static string GetHomeAccountId(AuthenticationRequestParameters requestParams, MsalTokenResponse response, IdToken idToken)
        {
            ClientInfo clientInfo = response.ClientInfo != null ? ClientInfo.CreateFromJson(response.ClientInfo) : null;
            string homeAccountId = clientInfo?.ToAccountIdentifier() ?? idToken?.Subject; // ADFS does not have client info, so we use subject

            if (homeAccountId == null)
            {
                requestParams.RequestContext.Logger.Info("Cannot determine home account id - or id token or no client info and no subject ");
            }
            return homeAccountId;
        }

        public static Dictionary<string, string> GetWamAccountIds(AuthenticationRequestParameters requestParams, MsalTokenResponse response)
        {
            if (!string.IsNullOrEmpty(response.WamAccountId))
            {
                return new Dictionary<string, string>() { { requestParams.AppConfig.ClientId, response.WamAccountId } };
            }

            return new Dictionary<string, string>();
        }
    }
}
