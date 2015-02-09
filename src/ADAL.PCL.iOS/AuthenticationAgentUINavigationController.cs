using System;
using CoreGraphics;

using CoreFoundation;
using UIKit;
using Foundation;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Foundation.Register("UniversalView")]
    public class UniversalView : UIView
    {
        public UniversalView()
        {
            Initialize();
        }

        public UniversalView(CGRect bounds)
            : base(bounds)
        {
            Initialize();
        }

        void Initialize()
        {
            BackgroundColor = UIColor.Red;
        }
    }

    [Foundation.Register("AuthenticationAgentUINavigationController")]
    internal class AuthenticationAgentUINavigationController : UINavigationController
    {
        private string url;
        private string callback;

        private AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod;

        public AuthenticationAgentUINavigationController(string url, string callback, AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
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
            this.PushViewController(new AuthenticationAgentUIViewController(this.url, this.callback, this.callbackMethod), true);
        }
    }
}