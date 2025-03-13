// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForClientParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// The SHA-256 hash of the access token that should be refreshed.
        /// If set, token refresh will occur only if a matching token is found in cache.
        /// </summary>
        public string AccessTokenHashToRefresh { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                var builder = new StringBuilder();
                builder.AppendLine("=== AcquireTokenForClientParameters ===");
                builder.AppendLine("SendX5C: " + SendX5C);
                builder.AppendLine("ForceRefresh: " + ForceRefresh);
                builder.AppendLine($"AccessTokenHashToRefresh: {!string.IsNullOrEmpty(AccessTokenHashToRefresh)}");
                logger.Info(builder.ToString());
            }
        }
    }
}
