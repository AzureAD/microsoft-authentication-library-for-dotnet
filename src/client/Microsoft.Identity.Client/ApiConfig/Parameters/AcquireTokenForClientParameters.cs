// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForClientParameters : IAcquireTokenParameters
    {
        /// <summary>
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// </summary>
        public bool SendX5C { get; set; }

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== AcquireTokenForClientParameters ===");
            builder.AppendLine("SendX5C: " + SendX5C);
            builder.AppendLine("ForceRefresh: " + ForceRefresh);
            logger.Info(builder.ToString());
        }
    }
}
