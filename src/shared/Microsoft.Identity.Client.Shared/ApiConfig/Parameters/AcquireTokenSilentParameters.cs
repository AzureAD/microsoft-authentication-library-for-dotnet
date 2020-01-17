// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenSilentParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }
        public bool SendX5C { get; set; }

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== OnBehalfOfParameters ===");
            builder.AppendLine("LoginHint provided: " + !string.IsNullOrEmpty(LoginHint));
            builder.AppendLine("User provided: " + (Account != null));
            builder.AppendLine("ForceRefresh: " + ForceRefresh);
            logger.Info(builder.ToString());
        }
    }
}
