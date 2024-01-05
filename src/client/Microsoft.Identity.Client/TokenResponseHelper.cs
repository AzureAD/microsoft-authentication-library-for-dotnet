// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

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

        public static string GetUsernameFromIdToken(IdToken idToken)
        {
            // The preferred_username value cannot be null or empty in order to comply with the ADAL/MSAL Unified cache schema.
            // It will be set to "Missing from the token response"
            if (idToken == null)
            {
                return NullPreferredUsernameDisplayLabel;
            }

            return idToken.PreferredUsername.NullIfWhiteSpace() ??
                   idToken.Upn.NullIfWhiteSpace() ??

#if !iOS // on iOS the username is used for caching, so better not to change this

                   idToken.Email.NullIfWhiteSpace() ??
                   idToken.Name.NullIfWhiteSpace() ??
#endif
                   NullPreferredUsernameDisplayLabel;
        }

        public static string GetHomeAccountId(AuthenticationRequestParameters requestParams, MsalTokenResponse response, IdToken idToken)
        {
            ClientInfo clientInfo = response.ClientInfo != null ? ClientInfo.CreateFromJson(response.ClientInfo) : null;
            string homeAccountId = clientInfo?.ToAccountIdentifier() ?? idToken?.Subject; // ADFS does not have client info, so we use subject

            if (homeAccountId == null)
            {
                requestParams.RequestContext.Logger.Info("Cannot determine home account ID - or id token or no client info and no subject ");
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
