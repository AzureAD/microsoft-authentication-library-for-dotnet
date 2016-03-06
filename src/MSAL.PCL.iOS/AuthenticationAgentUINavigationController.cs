//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CoreGraphics;

using CoreFoundation;
using UIKit;
using Foundation;

namespace Microsoft.Identity.Client
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
        private IDictionary<string, string> additionalHeaders;
        private AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod;

        public AuthenticationAgentUINavigationController(string url, string callback, IDictionary<string, string> additionalHeaders, AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
            this.additionalHeaders = additionalHeaders;
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
            CustomHeaderHandler.AdditionalHeaders = this.additionalHeaders;
            this.PushViewController(new AuthenticationAgentUIViewController(this.url, this.callback, this.callbackMethod), true);
        }
    }
}