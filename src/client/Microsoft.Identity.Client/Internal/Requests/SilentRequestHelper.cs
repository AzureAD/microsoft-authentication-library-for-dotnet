// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal
{
    internal static class SilentRequestHelper
    {
        internal static async Task<MsalTokenResponse> RefreshAccessTokenAsync(MsalRefreshTokenCacheItem msalRefreshTokenItem, RequestBase request, AuthenticationRequestParameters authenticationRequestParameters, CancellationToken cancellationToken)
        {
            authenticationRequestParameters.RequestContext.Logger.Verbose("Refreshing access token...");
            await authenticationRequestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

            var msalTokenResponse = await request.SendTokenRequestAsync(GetBodyParameters(msalRefreshTokenItem.Secret), cancellationToken)
                                    .ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                msalTokenResponse.RefreshToken = msalRefreshTokenItem.Secret;
                authenticationRequestParameters.RequestContext.Logger.Info(
                    "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead. ");
            }

            return msalTokenResponse;
        }

        private static Dictionary<string, string> GetBodyParameters(string refreshTokenSecret)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                [OAuth2Parameter.RefreshToken] = refreshTokenSecret
            };

            return dict;
        }

        internal static void ProcessFetchInBackgroundAsync(Func<Task<AuthenticationResult>> fetchAction, ICoreLogger logger)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await fetchAction().ConfigureAwait(false);
                }
                catch (MsalServiceException ex)
                {
                    string logMsg = $"Background fetch failed. Is AAD down? { ex.IsAadUnavailable()}. MsalServiceException {ex.ToString()}";
                    if (ex.StatusCode == 400)
                    {
                        logger.Error(logMsg);
                    }
                    else
                    {
                        logger.Warning(logMsg);
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning($"Background fetch failed Exception {ex.ToString()}");
                }
            });
        }
    }
}
