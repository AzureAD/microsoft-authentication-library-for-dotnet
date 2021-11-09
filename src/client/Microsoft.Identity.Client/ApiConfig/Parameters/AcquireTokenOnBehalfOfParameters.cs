﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenOnBehalfOfParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        /// <remarks>
        /// Is null when <see cref="ILongRunningWebApi.AcquireTokenInLongRunningProcess"/> is called.
        /// </remarks>
        public UserAssertion UserAssertion { get; set; }
        /// <summary>
        /// User-provided cache key for long-running OBO flow.
        /// </summary>
        public string OboCacheKey { get; set; }
        public bool ForceRefresh { get; set; }

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== OnBehalfOfParameters ===");
            builder.AppendLine("SendX5C: " + SendX5C);
            builder.AppendLine("ForceRefresh: " + ForceRefresh);
            builder.AppendLine("UserAssertion set: " + (UserAssertion != null));
            builder.AppendLine("OboCacheKey set: " + !string.IsNullOrWhiteSpace(OboCacheKey));
            logger.Info(builder.ToString());
        }
    }
}
