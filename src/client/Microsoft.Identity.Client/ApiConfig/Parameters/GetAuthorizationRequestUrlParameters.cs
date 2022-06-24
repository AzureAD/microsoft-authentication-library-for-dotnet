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
        public string CodeVerifier { get; set; }
        public KeyValuePair<string, string>? CcsRoutingHint { get; set; }
        public Prompt Prompt { get; set; } = Prompt.SelectAccount;

        public AcquireTokenInteractiveParameters ToInteractiveParameters()
        {
            return new AcquireTokenInteractiveParameters
            {
                Account = Account,
                ExtraScopesToConsent = ExtraScopesToConsent,
                LoginHint = LoginHint,
                Prompt = Prompt,
                UseEmbeddedWebView = WebViewPreference.NotSpecified,
                CodeVerifier = CodeVerifier
            };
        }

        /// <inheritdoc />
        public void LogParameters(ILoggerAdapter logger)
        {
        }
    }
}
