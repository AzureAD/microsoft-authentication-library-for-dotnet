// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    internal class InteractiveWebUI : WebUI
    {
        private WindowsFormsWebAuthenticationDialog _dialog;

        public InteractiveWebUI(CoreUIParent parent, RequestContext requestContext)
        {
            OwnerWindow = parent?.OwnerWindow;
            SynchronizationContext = parent?.SynchronizationContext;
            RequestContext = requestContext;
            EmbeddedWebViewOptions = parent?.EmbeddedWebviewOptions;
        }

        public EmbeddedWebViewOptions EmbeddedWebViewOptions { get; }

        protected override AuthorizationResult OnAuthenticate(CancellationToken cancellationToken)
        {
            AuthorizationResult result;

            using (_dialog = new WindowsFormsWebAuthenticationDialog(OwnerWindow, EmbeddedWebViewOptions) { RequestContext = RequestContext })
            {
                result = _dialog.AuthenticateAAD(RequestUri, CallbackUri, cancellationToken);
            }

            return result;
        }
    }
}
