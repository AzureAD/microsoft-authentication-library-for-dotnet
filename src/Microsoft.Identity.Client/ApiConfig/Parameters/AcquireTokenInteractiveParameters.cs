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
        public Prompt Prompt { get; set; } = Prompt.SelectAccount;
        public CoreUIParent UiParent { get; } = new CoreUIParent();
        public IEnumerable<string> ExtraScopesToConsent { get; set; } = new List<string>();
        public WebViewPreference UseEmbeddedWebView { get; set; } = WebViewPreference.NotSpecified;
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }
        public ICustomWebUi CustomWebUi { get; set; }

        public void LogParameters(ICoreLogger logger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== InteractiveParameters Data ===");
            builder.AppendLine("LoginHint provided: " + !string.IsNullOrEmpty(LoginHint));
            builder.AppendLine("User provided: " + (Account != null));
            builder.AppendLine("UseEmbeddedWebView: " + UseEmbeddedWebView);
            builder.AppendLine("ExtraScopesToConsent: " + string.Join(";", ExtraScopesToConsent ?? new List<string>()));
            builder.AppendLine("Prompt: " + Prompt.PromptValue);
            builder.AppendLine("HasCustomWebUi: " + (CustomWebUi != null));
            UiParent?.SystemWebViewOptions.LogParameters(logger);
            logger.Info(builder.ToString());
        }
    }
}
