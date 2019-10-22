// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !IS_APPCENTER_BUILD
using AuthenticationServices;
#endif
using Foundation;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.SystemWebview
{
    /* For app center builds, this will need to build on a hosted mac agent. The mac agent does not have the latest SDK's required to build 'ASWebAuthenticationSession'
* Until the agents are updated, appcenter build will need to ignore the use of 'ASWebAuthenticationSession' for iOS 12.*/

#if !IS_APPCENTER_BUILD
    internal class ASWebAuthenticationPresentationContextProviderWindow : NSObject, IASWebAuthenticationPresentationContextProviding
    {
        public UIWindow GetPresentationAnchor(ASWebAuthenticationSession session)
        {
            return UIApplication.SharedApplication.KeyWindow;
        }
    }
#endif
}
