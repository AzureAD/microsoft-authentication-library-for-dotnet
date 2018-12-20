//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using CoreGraphics;
using Microsoft.Identity.Client.Core;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    [Foundation.Register("UniversalView")]
    internal class UniversalView : UIView
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

        private void Initialize()
        {
            BackgroundColor = UIColor.Red;
        }
    }

    [Foundation.Register("AuthenticationAgentUINavigationController")]
    internal class AuthenticationAgentUINavigationController : UINavigationController
    {
        private readonly RequestContext _requestContext;
        private readonly string _url;
        private readonly string _callback;
        private readonly AuthenticationAgentUIViewController.ReturnCodeCallback _callbackMethod;
        private readonly UIStatusBarStyle _preferredStatusBarStyle;

        public AuthenticationAgentUINavigationController(
            RequestContext requestContext, 
            string url, 
            string callback, 
            AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod, 
            UIStatusBarStyle preferredStatusBarStyle)
        {
            _requestContext = requestContext;
            _url = url;
            _callback = callback;
            _callbackMethod = callbackMethod;
            _preferredStatusBarStyle = preferredStatusBarStyle;
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
            PushViewController(new AuthenticationAgentUIViewController(_requestContext, _url, _callback, _callbackMethod), true);
        }

        public override UIStatusBarStyle PreferredStatusBarStyle()
        {
            return _preferredStatusBarStyle;
        }
    }
}
