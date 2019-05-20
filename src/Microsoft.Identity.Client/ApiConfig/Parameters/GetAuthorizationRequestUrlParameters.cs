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

        public AcquireTokenInteractiveParameters ToInteractiveParameters()
        {
            return new AcquireTokenInteractiveParameters
            {
                Account = Account,
                ExtraScopesToConsent = ExtraScopesToConsent,
                LoginHint = LoginHint,
                Prompt = Prompt.SelectAccount,
                UseEmbeddedWebView = new Maybe<bool>()
            };
        }

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
        }
    }
}
