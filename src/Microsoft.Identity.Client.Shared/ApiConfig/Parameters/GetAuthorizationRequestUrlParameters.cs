// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class GetAuthorizationRequestUrlParameters : IAcquireTokenParameters
    {
        public string RedirectUri { get; set; }
        public IAccount Account { get; set; }
        public IEnumerable<string> ExtraScopesToConsent { get; set; }
        public string LoginHint { get; set; }

        public string Prompt { get; set; } 

       
        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
            logger.Info("=== GetAuthorizationRequestUrlParameters Parameters ===");
            logger.Info("LoginHint provided: " + !string.IsNullOrEmpty(LoginHint));
            logger.InfoPii(
                "Account provided: " + ((Account != null) ? Account.ToString() : "false"),
                "Account provided: " + (Account != null));
            logger.Info("Prompt: " + Prompt);
            logger.Info("RedirectUri: " + RedirectUri);
            logger.Info("ExtraScopesToConsent: " + string.Join(" ", ExtraScopesToConsent));
        }
    }
}
