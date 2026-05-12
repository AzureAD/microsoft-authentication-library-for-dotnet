// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForAgentParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        public AgentIdentity AgentIdentity { get; set; }

        public bool ForceRefresh { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                var builder = new StringBuilder();
                builder.AppendLine("=== AcquireTokenForAgentParameters ===");
                builder.AppendLine("SendX5C: " + SendX5C);
                builder.AppendLine("ForceRefresh: " + ForceRefresh);
                builder.AppendLine("AgentApplicationId: " + AgentIdentity?.AgentApplicationId);
                builder.AppendLine("HasUserIdentifier: " + (AgentIdentity?.HasUserIdentifier ?? false));
                logger.Info(builder.ToString());
            }
        }
    }
}
