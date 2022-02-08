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
            const string MAMEnrollmentIdKey = "microsoft_enrollment_id";
            authenticationRequestParameters.RequestContext.Logger.Verbose("Refreshing access token...");
            await authenticationRequestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

            var dict = GetBodyParameters(msalRefreshTokenItem.Secret);

            var enrollmentIDs = request.ServiceBundle.PlatformProxy.GetEnrollmentIds();
            
            if (!string.IsNullOrEmpty(enrollmentIDs))
            {
                var realEnrollmentId = ExtractEnrollmentId(enrollmentIDs);
                dict[MAMEnrollmentIdKey] = realEnrollmentId;
            }

            var msalTokenResponse = await request.SendTokenRequestAsync(dict, cancellationToken)
                                    .ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                msalTokenResponse.RefreshToken = msalRefreshTokenItem.Secret;
                authenticationRequestParameters.RequestContext.Logger.Warning(
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

        internal static bool NeedsRefresh(MsalAccessTokenCacheItem oldAccessToken)
        {
            return NeedsRefresh(oldAccessToken, out _);
        }

        internal static bool NeedsRefresh(MsalAccessTokenCacheItem oldAccessToken, out DateTimeOffset? refreshOnWithJitter)
        {
            refreshOnWithJitter = GetRefreshOnWithJitter(oldAccessToken);
            if (refreshOnWithJitter.HasValue && refreshOnWithJitter.Value < DateTimeOffset.UtcNow)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Fire and forget the fetch action on a background thread.
        /// Do not change to Task and do not await it.
        /// </summary>
        internal static void ProcessFetchInBackground(
            MsalAccessTokenCacheItem oldAccessToken,
            Func<Task<AuthenticationResult>> fetchAction,
            ICoreLogger logger)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await fetchAction().ConfigureAwait(false);
                }
                catch (MsalServiceException ex)
                {
                    string logMsg = $"Background fetch failed with MsalServiceException. Is AAD down? { ex.IsAadUnavailable()}";
                    if (ex.StatusCode == 400)
                    {
                        logger.ErrorPiiWithPrefix(ex, logMsg);
                    }
                    else
                    {
                        logger.WarningPiiWithPrefix(ex, logMsg);
                    }
                }
                catch (Exception ex)
                {
                    string logMsg = $"Background fetch failed with exception.";
                    logger.WarningPiiWithPrefix(ex, logMsg);
                }
            });
        }

        private static Random s_random = new Random();
        private static DateTimeOffset? GetRefreshOnWithJitter(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            if (msalAccessTokenCacheItem.RefreshOn.HasValue)
            {
                int jitter = s_random.Next(-Constants.DefaultJitterRangeInSeconds, Constants.DefaultJitterRangeInSeconds);
                var refreshOnWithJitter = msalAccessTokenCacheItem.RefreshOn.Value + TimeSpan.FromSeconds(jitter);
                return refreshOnWithJitter;
            }

            
            return null;           
        }

        private static string ExtractEnrollmentId(string enrollmentIDs)
        {
            // This method extracts EnrollmentId from EnrollmentIDs that correspoond ot the value in the keychain
            // expected input format is as follows
            //{
            //    "enrollment_ids" : [
            //      {
            //          "home_account_id" : "29ba73b9-5cbb-478b-a67d-1e8cf7dd425b.7257a09f-53cc-4a91-aca8-0cb6713642a5",
            //          "tid" : "7257a09f-53cc-4a91-aca8-0cb6713642a5",
            //          "user_id" : "IDLAB20TrueMAMCA@msidlab20.onmicrosoft.com",
            //          "oid" : "29ba73b9-5cbb-478b-a67d-1e8cf7dd425b",
            //          "enrollment_id" : "02460f14-a7e5-4458-9ab1-789d97114aae"
            //      }
            //    ]
            //}

            const string EnrollmentIdKey = "\"enrollment_id\"";

            // JSON conversion would be costly in performance and the format will stay unchanged.
            // So indexing has been used.
            // find the position of the key
            int indEnrollmentid = enrollmentIDs.IndexOf(EnrollmentIdKey);
            if (indEnrollmentid == -1)
            {
                return string.Empty;
            }
            // find the start of the value
            int beginrealEnrollmentid = enrollmentIDs.IndexOf('"', indEnrollmentid + EnrollmentIdKey.Length);
            // find end of the value
            int endrealEnrollmentid = enrollmentIDs.IndexOf('"', beginrealEnrollmentid + 1);
            // extract and return the value
            return enrollmentIDs.Substring(beginrealEnrollmentid + 1, endrealEnrollmentid - beginrealEnrollmentid - 1);
        }
    }
}
