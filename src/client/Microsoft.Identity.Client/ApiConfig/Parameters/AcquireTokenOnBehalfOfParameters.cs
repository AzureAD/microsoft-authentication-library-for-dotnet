// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenOnBehalfOfParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        /// <remarks>
        /// User assertion is null when <see cref="ILongRunningWebApi.InitiateLongRunningProcessInWebApi(IEnumerable{string}, string, ref string)"/> is called.
        /// </remarks>
        public UserAssertion UserAssertion { get; set; }
        /// <summary>
        /// User-provided cache key for long-running OBO flow.
        /// </summary>
        public string LongRunningOboCacheKey { get; set; }

        /// <summary>
        /// Only affects <see cref="ILongRunningWebApi.InitiateLongRunningProcessInWebApi(IEnumerable{string}, string, ref string)"/>.
        /// When enabled, mimics MSAL 4.50.0 and below behavior - checks in cache for cached tokens first, 
        /// and if not found, then uses user assertion to request new tokens from AAD.
        /// When disabled (default behavior), doesn't search in cache, but uses the user assertion to retrieve tokens from AAD.
        /// </summary>
        public bool SearchInCacheForLongRunningObo { get; set; }

        public bool ForceRefresh { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                var builder = new StringBuilder(
                    $"""
                    === OnBehalfOfParameters ===
                    SendX5C: {SendX5C}
                    ForceRefresh: {ForceRefresh}
                    UserAssertion set: {UserAssertion != null}
                    SearchInCacheForLongRunningObo: {SearchInCacheForLongRunningObo}
                    LongRunningOboCacheKey set: {!string.IsNullOrWhiteSpace(LongRunningOboCacheKey)}
                    """);
                if (UserAssertion != null && !string.IsNullOrWhiteSpace(LongRunningOboCacheKey))
                {
                    builder.AppendLine("InitiateLongRunningProcessInWebApi called: True");
                }
                else if (UserAssertion == null && !string.IsNullOrWhiteSpace(LongRunningOboCacheKey))
                {
                    builder.AppendLine("AcquireTokenInLongRunningProcess called: True");
                }
                logger.Info(builder.ToString());
            }
        }
    }
}
