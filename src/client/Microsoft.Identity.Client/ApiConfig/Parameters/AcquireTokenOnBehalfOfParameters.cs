// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenOnBehalfOfParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        /// <remarks>
        /// User assertion is null when <see cref="ILongRunningWebApi.AcquireTokenInLongRunningProcess"/> is called.
        /// </remarks>
        public UserAssertion UserAssertion { get; set; }
        /// <summary>
        /// User-provided cache key for long-running OBO flow.
        /// </summary>
        public string LongRunningOboCacheKey { get; set; }
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
