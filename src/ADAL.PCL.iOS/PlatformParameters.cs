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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UIKit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Additional parameters used in acquiring user's authorization
    /// </summary>
    public class PlatformParameters : IPlatformParameters
    {
        private PlatformParameters()
        {
            UseBroker = false;
        }

        /// <summary>
        /// Additional parameters used in acquiring user's authorization
        /// </summary>
        /// <param name="callerViewController">UIViewController instance</param>
        public PlatformParameters(UIViewController callerViewController):this()
        {
            this.CallerViewController = callerViewController;
        }

        /// <summary>
        /// Additional parameters used in acquiring user's authorization
        /// </summary>
        /// <param name="callerViewController">UIViewController instance</param>
        /// <param name="useBroker">skips calling to broker if broker is present. false, by default</param>
        public PlatformParameters(UIViewController callerViewController, bool useBroker):this(callerViewController)
        {
            UseBroker = useBroker;
        }

        /// <summary>
        /// Caller UIViewController
        /// </summary>
        public UIViewController CallerViewController { get; private set; }

        /// <summary>
        /// Skips calling to broker if broker is present. false, by default
        /// </summary>
        public bool UseBroker { get; set; }

        /// <summary>
        /// Sets the preferred status bar style for the login form view controller presented
        /// </summary>
        /// <value>The preferred status bar style.</value>
        public UIStatusBarStyle PreferredStatusBarStyle { get; set; }

        /// <summary>
        /// Set the transition style used when the login form view is presented
        /// </summary>
        /// <value>The modal transition style.</value>
        public UIModalTransitionStyle ModalTransitionStyle { get; set; }

        /// <summary>
        /// Sets the presentation style used when the login form view is presented
        /// </summary>
        /// <value>The modal presentation style.</value>
        public UIModalPresentationStyle ModalPresentationStyle { get; set; }


        /// <summary>
        /// Sets a custom transitioning delegate to the login form view controller
        /// </summary>
        /// <value>The transitioning delegate.</value>
        public UIViewControllerTransitioningDelegate TransitioningDelegate { get; set; }
    }
}
