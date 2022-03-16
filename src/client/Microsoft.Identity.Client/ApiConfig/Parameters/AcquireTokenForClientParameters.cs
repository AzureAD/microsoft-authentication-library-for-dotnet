// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForClientParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        /// <summary>
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <inheritdoc />
        public void LogParameters(IMsalLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== AcquireTokenForClientParameters ===");
            builder.AppendLine("SendX5C: " + SendX5C);
            builder.AppendLine("ForceRefresh: " + ForceRefresh);
            logger.Info(builder.ToString());
        }
    }
}
