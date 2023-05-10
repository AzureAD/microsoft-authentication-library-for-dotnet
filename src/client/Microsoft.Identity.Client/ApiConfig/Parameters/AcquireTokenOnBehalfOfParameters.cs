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
        /// When enabled, mimics MSAL 4.50.0 and below behavior - does not check cached tokens based on OBO assertions.
        /// When disabled (default behavior), cached tokens will only be returned if the OBO assertion in the request matched the assertion of the cached token.
        /// </summary>
        public bool IgnoreCachedOboAssertion { get; set; }

        public bool ForceRefresh { get; set; }

        /// <inheritdoc />
        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                var builder = new StringBuilder();
                builder.AppendLine("=== OnBehalfOfParameters ===");
                builder.AppendLine("SendX5C: " + SendX5C);
                builder.AppendLine("ForceRefresh: " + ForceRefresh);
                builder.AppendLine("UserAssertion set: " + (UserAssertion != null));
                builder.AppendLine("IgnoreCachedOboAssertion: " + IgnoreCachedOboAssertion);
                builder.AppendLine("LongRunningOboCacheKey set: " + !string.IsNullOrWhiteSpace(LongRunningOboCacheKey));
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
