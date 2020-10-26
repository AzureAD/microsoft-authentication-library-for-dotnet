// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CoreGraphics;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    [Foundation.Register("MsalUniversalView")]
    internal class MsalUniversalView : UIView
    {
        public MsalUniversalView()
        {
            Initialize();
        }

        public MsalUniversalView(CGRect bounds)
            : base(bounds)
        {
            Initialize();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Red;
        }
    }

    [Foundation.Register("MsalAuthenticationAgentUINavigationController")]
    internal class MsalAuthenticationAgentUINavigationController : UINavigationController
    {
        private readonly string url;
        private readonly string callback;

        private readonly MsalAuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod;

        private readonly UIStatusBarStyle preferredStatusBarStyle;

        public MsalAuthenticationAgentUINavigationController(string url, string callback, MsalAuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod, UIStatusBarStyle preferredStatusBarStyle)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
            this.preferredStatusBarStyle = preferredStatusBarStyle;
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Perform any additional setup after loading the view
            this.PushViewController(new MsalAuthenticationAgentUIViewController(this.url, this.callback, this.callbackMethod), true);
        }

        public override UIStatusBarStyle PreferredStatusBarStyle()
        {
            return this.preferredStatusBarStyle;
        }
    }
}
