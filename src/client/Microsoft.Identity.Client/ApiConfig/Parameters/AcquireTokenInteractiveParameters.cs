// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{

    internal class AcquireTokenInteractiveParameters : IAcquireTokenParameters
    {
        public Prompt Prompt { get; set; } = Prompt.NotSpecified;
        public CoreUIParent UiParent { get; } = new CoreUIParent();

        /// <summary>
        ///  These need to be asked for to the /authorize endpoint (for consent)
        ///  but not to the /token endpoint
        /// </summary>
        public IEnumerable<string> ExtraScopesToConsent { get; set; } = CollectionHelpers.GetEmptyReadOnlyList<string>();
        public WebViewPreference UseEmbeddedWebView { get; set; } = WebViewPreference.NotSpecified;
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }
        public ICustomWebUi CustomWebUi { get; set; }
        public string CodeVerifier { get; set; }

        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                UiParent.SystemWebViewOptions?.LogParameters(logger);
                logger.Info(
                    $"""
                     === InteractiveParameters Data ===
                     LoginHint provided: {!string.IsNullOrEmpty(LoginHint)}
                     User provided: {Account != null}
                     UseEmbeddedWebView: {UseEmbeddedWebView}
                     ExtraScopesToConsent: {string.Join(";", ExtraScopesToConsent ?? CollectionHelpers.GetEmptyReadOnlyList<string>())}
                     Prompt: {Prompt.PromptValue}
                     HasCustomWebUi: {CustomWebUi != null}
                     """);
            }
        }
    }
}
