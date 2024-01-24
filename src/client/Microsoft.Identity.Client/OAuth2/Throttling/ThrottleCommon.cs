// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal static class ThrottleCommon
    {
        public const string ThrottleRetryAfterHeaderName = "x-ms-lib-capability";
        public const string ThrottleRetryAfterHeaderValue = "retry-after, h429";

        internal const char KeyDelimiter = '.';

        /// <summary>
        /// The strict thumbprint is based on: 
        /// ClientId
        /// Authority
        /// Resource
        /// Scope
        /// Account
        /// </summary>
        public static string GetRequestStrictThumbprint(
            IReadOnlyDictionary<string, string> bodyParams,
            string authority,
            string homeAccountId)
        {
            var sb = new StringBuilder();
            if (bodyParams.TryGetValue(OAuth2Parameter.ClientId, out string clientId))
            {
                sb.Append(clientId);
                sb.Append(KeyDelimiter);
            }
            sb.Append(authority);
            sb.Append(KeyDelimiter);
            if (bodyParams.TryGetValue(OAuth2Parameter.Scope, out string scopes))
            {
                sb.Append(scopes);
                sb.Append(KeyDelimiter);
            }

            sb.Append(homeAccountId);
            sb.Append(KeyDelimiter);

            return sb.ToString();
        }

        public static void TryThrowServiceException(string thumbprint, ThrottlingCache cache, ILoggerAdapter logger, string providerName)
        {
            if (cache.TryGetOrRemoveExpired(thumbprint, logger, out var ex))
            {
                logger.WarningPii(
                    $"[Throttling] Exception thrown because of throttling rule {providerName} - thumbprint: {thumbprint}",
                    $"[Throttling] Exception thrown because of throttling rule {providerName}");

                // mark the exception for logging purposes                
                throw new MsalThrottledServiceException(ex);
            }
        }               
    }
}
