// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.Cache
{
    internal static class SuggestedWebCacheKeyFactory
    {
        public static string GetKeyFromRequest(AuthenticationRequestParameters requestParameters)
        {
            if (GetOboOrAppKey(requestParameters, out string key))
            {
                return key;
            }

            if (requestParameters.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenSilent)
            {
                return requestParameters.Account?.HomeAccountId?.Identifier;
            }

            if (requestParameters.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.GetAccountById)
            {
                return requestParameters.HomeAccountId; 
            }

            return null;

        }

        public static string GetKeyFromResponse(AuthenticationRequestParameters requestParameters, string homeAccountIdFromResponse)
        {
            if (GetOboOrAppKey(requestParameters, out string key))
            {
                return key;
            }

            if (requestParameters.IsConfidentialClient || 
                requestParameters.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenSilent)
            {
                return homeAccountIdFromResponse;
            }

            return null;
        }

        private static bool GetOboOrAppKey(AuthenticationRequestParameters requestParameters, out string key)
        {
            if (requestParameters.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenOnBehalfOf)
            {
                key = requestParameters.UserAssertion.AssertionHash;
                return true;
            }

            if (requestParameters.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenForClient)
            {
                if (!string.IsNullOrEmpty(requestParameters.Authority.TenantId) &&
                    Guid.TryParse(requestParameters.Authority.TenantId, out var tenantId))
                {
                    key = $"{requestParameters.AppConfig.ClientId}_{tenantId:D}_AppTokenCache";
                    return true;
                }

                key = requestParameters.AppConfig.ClientId + "_AppTokenCache";
                return true;
            }

            key = null;
            return false;
        }
    }
}
