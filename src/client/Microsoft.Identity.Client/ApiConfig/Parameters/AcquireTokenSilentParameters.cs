// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenSilentParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }
        public bool? SendX5C { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsInfoOrVerboseEnabled())
            {
                logger.InfoOrVerbose("=== AcquireTokenSilent Parameters ===");
                logger.InfoOrVerbose("LoginHint provided: " + !string.IsNullOrEmpty(LoginHint));
                logger.InfoOrVerbosePii(
                    "Account provided: " + ((Account != null) ? Account.ToString() : "false"),
                    "Account provided: " + (Account != null));
                logger.InfoOrVerbose("ForceRefresh: " + ForceRefresh);
            }
        }
    }
}
