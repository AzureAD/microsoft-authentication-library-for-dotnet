// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.net45
{
    internal class InteractiveWebUI : WebUI
    {
        private WindowsFormsWebAuthenticationDialog _dialog;

        public InteractiveWebUI(CoreUIParent parent, RequestContext requestContext)
        {
            OwnerWindow = parent?.OwnerWindow;
            SynchronizationContext = parent?.SynchronizationContext;
            RequestContext = requestContext;
        }

        protected override AuthorizationResult OnAuthenticate()
        {
            AuthorizationResult result;

            using (_dialog = new WindowsFormsWebAuthenticationDialog(OwnerWindow) {RequestContext = RequestContext})
            {
                result = _dialog.AuthenticateAAD(RequestUri, CallbackUri);
            }

            return result;
        }
    }
}
