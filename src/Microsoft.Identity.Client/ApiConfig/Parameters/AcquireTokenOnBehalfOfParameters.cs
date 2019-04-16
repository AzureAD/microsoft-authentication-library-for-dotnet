// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenOnBehalfOfParameters : IAcquireTokenParameters
    {
        public UserAssertion UserAssertion { get; set; }
        public bool SendX5C { get; set;}

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== OnBehalfOfParameters ===");
            builder.AppendLine("SendX5C: " + SendX5C);
            logger.Info(builder.ToString());
        }
    }
}
