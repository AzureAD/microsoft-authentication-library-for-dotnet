// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal class UiRequiredProvider : IThrottlingProvider
    {
        /// <summary>
        /// Default number of seconds that application returns the cached response, in case of UI required requests.
        /// </summary>
        internal static readonly TimeSpan s_uiRequiredExpiration = TimeSpan.FromSeconds(120);

        /// <summary>
        /// Exposed only for testing purposes
        /// </summary>
        internal ThrottlingCache ThrottlingCache { get; }

        public UiRequiredProvider()
        {
            ThrottlingCache = new ThrottlingCache();
        }

        public void RecordException(AuthenticationRequestParameters requestParams, IReadOnlyDictionary<string, string> bodyParams, MsalServiceException ex)
        {
            if (ex is MsalUiRequiredException && IsRequestSupported(requestParams))
            {
                var logger = requestParams.RequestContext.Logger;

                logger.Info(() => $"[Throttling] MsalUiRequiredException encountered - " +
                    $"throttling for {s_uiRequiredExpiration.TotalSeconds} seconds. ");

                var thumbprint = GetRequestStrictThumbprint(bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority.ToString(),
                    requestParams.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager);
                var entry = new ThrottlingCacheEntry(ex, s_uiRequiredExpiration);
                ThrottlingCache.AddAndCleanup(thumbprint, entry, logger);
            }
        }

        public void ResetCache()
        {
            ThrottlingCache.Clear();
        }

        public void TryThrottle(AuthenticationRequestParameters requestParams, IReadOnlyDictionary<string, string> bodyParams)
        {
            if (!ThrottlingCache.IsEmpty() && IsRequestSupported(requestParams))
            {
                var logger = requestParams.RequestContext.Logger;

                string fullThumbprint = GetRequestStrictThumbprint(
                    bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority.ToString(),
                    requestParams.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager);

                TryThrowException(fullThumbprint, logger);
            }
        }

        private void TryThrowException(string thumbprint, ILoggerAdapter logger)
        {
            if (ThrottlingCache.TryGetOrRemoveExpired(thumbprint, logger, out MsalServiceException ex) &&
                ex is MsalUiRequiredException uiException)
            {
                logger.WarningPii(
                    $"[Throttling] Exception thrown because of throttling rule UiRequired - thumbprint: {thumbprint}",
                    $"[Throttling] Exception thrown because of throttling rule UiRequired ");

                // mark the exception for logging purposes
                throw new MsalThrottledUiRequiredException(uiException);
            }
        }

        /// <summary>
        /// MsalUiRequiredException is thrown from AcquireTokenSilent, based on certain error codes from the server 
        /// when contacting the token endpoint.
        /// Currently, throttling will only apply to public client applications at first. 
        /// </summary>
        private static bool IsRequestSupported(AuthenticationRequestParameters requestParams)
        {
            return !requestParams.AppConfig.IsConfidentialClient &&
                requestParams.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenSilent;
        }

        /// <summary>
        /// The strict thumbprint is based on: 
        /// ClientId
        /// Authority (env + tenant)
        /// Scopes
        /// hash(RT) or UPN for IWA (not supported)
        /// </summary>
        private static string GetRequestStrictThumbprint(
            IReadOnlyDictionary<string, string> bodyParams,
            string authority,
            ICryptographyManager crypto)
        {
            var sb = new StringBuilder();
            if (bodyParams.TryGetValue(OAuth2Parameter.ClientId, out string clientId))
            {
                sb.Append(clientId);
                sb.Append(ThrottleCommon.KeyDelimiter);
            }
            sb.Append(authority);
            sb.Append(ThrottleCommon.KeyDelimiter);
            if (bodyParams.TryGetValue(OAuth2Parameter.Scope, out string scopes))
            {
                sb.Append(scopes);
                sb.Append(ThrottleCommon.KeyDelimiter);
            }

            if (bodyParams.TryGetValue(OAuth2Parameter.RefreshToken, out string rt) &&
                !string.IsNullOrEmpty(rt))
            {
                sb.Append(crypto.CreateSha256Hash(rt));
                sb.Append(ThrottleCommon.KeyDelimiter);
            }

            // check mam enrollment id
            if (bodyParams.TryGetValue(SilentRequestHelper.MamEnrollmentIdKey, out string mamEnrollmentId))
            {
                sb.Append(crypto.CreateSha256Hash(mamEnrollmentId));
                sb.Append(ThrottleCommon.KeyDelimiter);
            }

            return sb.ToString();
        }
    }
}
