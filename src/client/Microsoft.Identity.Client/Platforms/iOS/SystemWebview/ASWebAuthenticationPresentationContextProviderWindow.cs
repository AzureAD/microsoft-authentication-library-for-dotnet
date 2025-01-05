// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using AuthenticationServices;
using Foundation;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.SystemWebview
{
    internal class ASWebAuthenticationPresentationContextProviderWindow : NSObject, IASWebAuthenticationPresentationContextProviding
    {
        public UIWindow GetPresentationAnchor(ASWebAuthenticationSession session)
        {
#pragma warning disable CA1422 // Validate platform compatibility
            return UIApplication.SharedApplication.KeyWindow;
#pragma warning restore CA1422 // Validate platform compatibility
        }
    }
}
