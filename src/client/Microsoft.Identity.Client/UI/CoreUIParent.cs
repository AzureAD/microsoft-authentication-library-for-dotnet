// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

using System.Threading;
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Client.UI
{
    internal class CoreUIParent
    {
        public CoreUIParent()
        {

        }

        internal SynchronizationContext SynchronizationContext { get; set; }

        internal SystemWebViewOptions SystemWebViewOptions { get; set; }
        internal EmbeddedWebViewOptions EmbeddedWebviewOptions { get; set; }

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

        internal static UIViewController FindCurrentViewController(UIViewController callerViewController)
        {
            if (callerViewController is UITabBarController)
            {
                UITabBarController tabBarController = (UITabBarController)callerViewController;
                return FindCurrentViewController(tabBarController.SelectedViewController);
            }
            else if (callerViewController is UINavigationController uiNavigationController && uiNavigationController.VisibleViewController != null)
            {
                return FindCurrentViewController(uiNavigationController.VisibleViewController);
            }
            else if (callerViewController.PresentedViewController != null)
            {
                UIViewController presentedViewController = callerViewController.PresentedViewController;
                return FindCurrentViewController(presentedViewController);
            }
            else
            {
                return callerViewController;
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

#if NETFRAMEWORK || WINDOWS_APP
        //hidden webview can be used in both WinRT and desktop applications.
        internal bool UseHiddenBrowser { get; set; }
#endif

#if WINDOWS_APP
        internal bool UseCorporateNetwork { get; set; }
#endif

#if NETFRAMEWORK || NET6_WIN || NET_CORE || NETSTANDARD
        internal object OwnerWindow { get; set; }       
#endif
    }
}
