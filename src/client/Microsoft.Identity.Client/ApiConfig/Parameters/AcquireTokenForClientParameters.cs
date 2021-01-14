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

        /// <summary>
        /// When set to true, the request is sent to regional endpoint.
        /// </summary>
        public bool AutoDetectRegion { get; set; }

        /// <summary>
        /// This field wil contain the region provided by user and will be used along with region auto detection.
        /// </summary>
        public string RegionToUse { get; set; }

        /// <summary>
        /// </summary>
        public bool FallbackToGlobal { get; set; }

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== AcquireTokenForClientParameters ===");
            builder.AppendLine("SendX5C: " + SendX5C);
            builder.AppendLine("WithAzureRegion: " + AutoDetectRegion);
            builder.AppendLine("RegionToUse: " + RegionToUse);
            builder.AppendLine("ForceRefresh: " + ForceRefresh);
            logger.Info(builder.ToString());
        }
    }
}
