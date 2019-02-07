//----------------------------------------------------------------------
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

#if ANDROID
using System;
using Android.App;
#endif
#if iOS
using UIKit;
#endif
#if MAC
using AppKit;
#endif

namespace Microsoft.Identity.Client.UI
{
    internal class CoreUIParent //TODO bogavril: break this class up into platform specific classes or merge with UIParent (probably merge!)
    {
        public CoreUIParent()
        {
        }

        internal bool UseEmbeddedWebview { get; set; }

#if MAC
        /// <summary>
        /// Initializes an instance for a provided caller window.
        /// </summary>
        /// <param name="callerWindow">Caller window. OPTIONAL.</param>
        public CoreUIParent(NSWindow callerWindow)
        {
            CallerWindow = callerWindow;
        }

        /// <summary>
        /// Caller NSWindow
        /// </summary>
        public NSWindow CallerWindow { get; set; }
#endif

#if iOS
        /// <summary>
        /// Initializes an instance for a provided caller window.
        /// </summary>
        /// <param name="callerWindow">Caller window. OPTIONAL.</param>
        public CoreUIParent(UIViewController callerWindow)
        {
            CallerViewController = callerWindow;
        }

        /// <summary>
        /// Caller UIViewController
        /// </summary>
        public UIViewController CallerViewController { get; set; }

        internal static UIViewController FindCurrentViewController(UIViewController CallerViewController)
        {
            if (CallerViewController is UITabBarController)
            {
                UITabBarController tabBarController = (UITabBarController)CallerViewController;
                return FindCurrentViewController(tabBarController.SelectedViewController);
            }
            else if (CallerViewController is UINavigationController)
            {
                UINavigationController navigationController = (UINavigationController)CallerViewController;
                return FindCurrentViewController(navigationController.VisibleViewController);
            }
            else if (CallerViewController.PresentedViewController != null)
            {
                UIViewController presentedViewController = CallerViewController.PresentedViewController;
                return FindCurrentViewController(presentedViewController);
            }
            else
            {
                return CallerViewController;
            }
        }

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
#endif

#if ANDROID
        internal Activity Activity { get; set; }
        /// <summary>
        /// Initializes an instance for a provided activity.
        /// </summary>
        /// <param name="activity">parent activity for the call. REQUIRED.</param>
        public CoreUIParent(Activity activity)
        {
           if(activity == null)
           {		
                throw new ArgumentException("passed in activity is null", nameof(activity));		
           }
            Activity = activity;
            CallerActivity = activity;
        }

        /// <summary>
        /// Caller Android Activity
        /// </summary>
        public Activity CallerActivity { get; set; }
#endif

#if DESKTOP || WINDOWS_APP
        //hidden webview can be used in both WinRT and desktop applications.
        internal bool UseHiddenBrowser { get; set; }
#endif

#if WINDOWS_APP
        internal bool UseCorporateNetwork { get; set; }
#endif

#if DESKTOP
        internal object OwnerWindow { get; set; }

        /// <summary>
        /// Initializes an instance for a provided parent window.
        /// </summary>
        /// <param name="ownerWindow">Parent window object reference. OPTIONAL.</param>
        public CoreUIParent(object ownerWindow)
        {
            OwnerWindow = ownerWindow;
        }
#endif
    }
}