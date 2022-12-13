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

    internal class AcquireTokenInteractiveParameters : GetAuthorizationRequestUrlParameters, IAcquireTokenParameters
    {
        public CoreUIParent UiParent { get; } = new CoreUIParent();

        /// <summary>
        ///  These need to be asked for to the /authorize endpoint (for consent)
        ///  but not to the /token endpoint
        /// </summary>
        public WebViewPreference UseEmbeddedWebView { get; set; } = WebViewPreference.NotSpecified;
        public ICustomWebUi CustomWebUi { get; set; }

        public new void LogParameters(ILoggerAdapter logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== InteractiveParameters Data ===");
            builder.AppendLine("LoginHint provided: " + !string.IsNullOrEmpty(LoginHint));
            builder.AppendLine("User provided: " + (Account != null));
            builder.AppendLine("UseEmbeddedWebView: " + UseEmbeddedWebView);
            builder.AppendLine("ExtraScopesToConsent: " + string.Join(";", ExtraScopesToConsent ?? CollectionHelpers.GetEmptyReadOnlyList<string>()));
            builder.AppendLine("Prompt: " + Prompt.PromptValue);
            builder.AppendLine("HasCustomWebUi: " + (CustomWebUi != null));
            UiParent.SystemWebViewOptions?.LogParameters(logger);
            logger.Info(builder.ToString());
        }
    }
}
