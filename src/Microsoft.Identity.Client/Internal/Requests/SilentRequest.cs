// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Mats.Internal.Events;
using System.Linq;
using System;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentRequest : RequestBase
    {
        private readonly AcquireTokenSilentParameters _silentParameters;
        private const string TheOnlyFamilyId = "1";
        private const string FociClientMismatchSubError = "client_mismatch";

        public SilentRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters)
            : base(serviceBundle, authenticationRequestParameters, silentParameters)
        {
            _silentParameters = silentParameters;
        }

        private async Task<IAccount> GetSingleAccountForLoginHintAsync(string loginHint)
        {
            var accounts = await CacheManager.TokenCacheInternal.GetAccountsAsync(
                ServiceBundle.Config.AuthorityInfo.CanonicalAuthority,
                AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            accounts = accounts
                .Where(a => !string.IsNullOrWhiteSpace(a.Username) &&
                       a.Username.Equals(loginHint, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!accounts.Any())
            {
                throw new MsalUiRequiredException(
                    MsalError.NoAccountForLoginHint,
                    MsalErrorMessage.NoAccountForLoginHint);
            }

            if (accounts.Count() > 1)
            {
                throw new MsalUiRequiredException(
                    MsalError.MultipleAccountsForLoginHint,
                    MsalErrorMessage.MultipleAccountsForLoginHint);
            }

            return accounts.First();
        }

        private async Task<IAccount> GetAccountFromParamsOrLoginHintAsync(AcquireTokenSilentParameters silentParameters)
        {
            if (silentParameters.Account != null)
            {
                return silentParameters.Account;
            }

            return await GetSingleAccountForLoginHintAsync(silentParameters.LoginHint).ConfigureAwait(false);
        }

        internal async override Task PreRunAsync()
        {
            IAccount account = await GetAccountFromParamsOrLoginHintAsync(_silentParameters).ConfigureAwait(false);
            AuthenticationRequestParameters.Account = account;

            AuthenticationRequestParameters.Authority = AuthenticationRequestParameters.AuthorityOverride == null
                ? ClientApplicationBase.GetAuthority(ServiceBundle, account)
                : Instance.Authority.CreateAuthorityWithOverride(
                    ServiceBundle,
                    AuthenticationRequestParameters.AuthorityOverride);
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CacheManager.HasCache)
            {
                throw new MsalUiRequiredException(
                    MsalError.TokenCacheNullError,
                    MsalErrorMessage.NullTokenCacheForSilentError);
            }

            // Look for access token
            if (!_silentParameters.ForceRefresh)
            {
                MsalAccessTokenCacheItem msalAccessTokenItem =
                    await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

                if (msalAccessTokenItem != null)
                {
                    var msalIdTokenItem = await CacheManager.GetIdTokenCacheItemAsync(msalAccessTokenItem.GetIdTokenItemKey()).ConfigureAwait(false);
                    return new AuthenticationResult(msalAccessTokenItem, msalIdTokenItem);
                }
            }

            // Try FOCI first
            MsalTokenResponse msalTokenResponse = await TryGetTokenUsingFociAsync(cancellationToken)
                .ConfigureAwait(false);

            // Normal, non-FOCI flow
            if (msalTokenResponse == null)
            {
                // Look for a refresh token
                MsalRefreshTokenCacheItem appRefreshToken = await FindRefreshTokenOrFailAsync()
                    .ConfigureAwait(false);

                msalTokenResponse = await RefreshAccessTokenAsync(appRefreshToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> TryGetTokenUsingFociAsync(CancellationToken cancellationToken)
        {
            if (!ServiceBundle.PlatformProxy.GetFeatureFlags().IsFociEnabled)
            {
                return null;
            }

            var logger = AuthenticationRequestParameters.RequestContext.Logger;

            // If the app was just added to the family, the app metadata will reflect this
            // after the first RT exchanged.
            bool? isFamilyMember = await CacheManager.IsAppFociMemberAsync(TheOnlyFamilyId).ConfigureAwait(false);

            if (isFamilyMember.HasValue && isFamilyMember.Value == false)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Verbose(
                    "[FOCI] App is not part of the family, skipping FOCI.");

                return null;
            }

            logger.Verbose("[FOCI] App is part of the family or unkown, looking for FRT");
            var familyRefreshToken = await CacheManager.FindFamilyRefreshTokenAsync(TheOnlyFamilyId).ConfigureAwait(false);
            logger.Verbose("[FOCI] FRT found? " + (familyRefreshToken != null));

            if (familyRefreshToken != null)
            {
                try
                {
                    MsalTokenResponse frtTokenResponse = await RefreshAccessTokenAsync(familyRefreshToken, cancellationToken)
                        .ConfigureAwait(false);

                    logger.Verbose("[FOCI] FRT refresh succeeded");
                    return frtTokenResponse;
                }
                catch (MsalServiceException ex)
                {
                    // Hack: STS does not yet send back the suberror on these platforms because they are not in an allowed list,
                    // so the best thing we can do is to consider all errors as client_mismatch.
#if NETSTANDARD || WINDOWS_APP || MAC
                    ex?.GetType();  // avoid the "variable 'ex' is declared but never used" in this code path.
                    return null;
#else
                    if (MsalError.InvalidGrantError.Equals(ex?.ErrorCode, StringComparison.OrdinalIgnoreCase) &&
                        FociClientMismatchSubError.Equals(ex?.SubError, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Error("[FOCI] FRT refresh failed - client mismatch");
                        return null;
                    }

                    // Rethrow failures to refresh the FRT, other than client_mismatch, because
                    // apps need to handle them in the same way they handle exceptions from refreshing the RT.
                    // For example, some apps have special handling for MFA errors.
                    logger.Error("[FOCI] FRT refresh failed - other error");
                    throw;
#endif
                }
            }

            return null;

        }

        private async Task<MsalTokenResponse> RefreshAccessTokenAsync(MsalRefreshTokenCacheItem msalRefreshTokenItem, CancellationToken cancellationToken)
        {
            AuthenticationRequestParameters.RequestContext.Logger.Verbose("Refreshing access token...");
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);

            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(msalRefreshTokenItem.Secret), cancellationToken)
                                    .ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                msalTokenResponse.RefreshToken = msalRefreshTokenItem.Secret;
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            return msalTokenResponse;
        }

        private async Task<MsalRefreshTokenCacheItem> FindRefreshTokenOrFailAsync()
        {
            var msalRefreshTokenItem = await CacheManager.FindRefreshTokenAsync().ConfigureAwait(false);
            if (msalRefreshTokenItem == null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Verbose("No Refresh Token was found in the cache");

                throw new MsalUiRequiredException(
                    MsalError.NoTokensFoundError,
                    MsalErrorMessage.NoTokensFoundError);
            }

            return msalRefreshTokenItem;
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            if (_silentParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _silentParameters.LoginHint;
            }
        }

        private Dictionary<string, string> GetBodyParameters(string refreshTokenSecret)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                [OAuth2Parameter.RefreshToken] = refreshTokenSecret
            };

            return dict;
        }
    }
}
