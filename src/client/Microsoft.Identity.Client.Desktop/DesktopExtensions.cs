﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;

namespace Microsoft.Identity.Client.Desktop
{
    /// <summary>
    /// MSAL extensions for desktop apps
    /// </summary>
    public static class DesktopExtensions
    {
        /// <summary>
        /// Adds enhanced support for desktop applications, e.g. CLI, WinForms, WPF apps.
        /// 
        /// Support added is around: 
        /// 
        /// - Windows Authentication Manager (WAM) broker, the recommended authentication mechanism on Windows 10 - https://aka.ms/msal-net-wam
        /// - WebView2 embedded web view, based on Microsoft Edge - https://aka.ms/msal-net-webview2
        /// </summary>
        /// <remarks>These extensions live in a separate package to avoid adding dependencies to MSAL</remarks>
        [Obsolete("This method has been deprecated. Use WithWindowsDesktopFeatures. For Windows Broker support only, use WithWindowsBroker API from Microsoft.Identity.Client.Broker package.", false)]
        public static PublicClientApplicationBuilder WithDesktopFeatures(this PublicClientApplicationBuilder builder)
        {
            BrokerExtension.WithWindowsBroker(builder);
            AddSupportForWebView2(builder);

            return builder;
        }

        /// <summary>
        /// Adds enhanced support for desktop applications, e.g. CLI, WinForms, WPF apps.
        /// - Windows Authentication Manager (WAM) broker, the recommended authentication mechanism on Windows 10 - https://aka.ms/msal-net-wam
        /// - Embedded web view. AAD applications use the older WebBrowser control. B2C applications use WebView2, an embedded browser based on Microsoft Edge - https://aka.ms/msal-net-webview2
        /// </summary>
        /// <remarks>These extensions live in a separate package to avoid adding dependencies to MSAL</remarks>
        public static PublicClientApplicationBuilder WithWindowsDesktopFeatures(this PublicClientApplicationBuilder builder)
        {
            BrokerExtension.WithWindowsBroker(builder);
            AddSupportForWebView2(builder);

            return builder;
        }

        /// <summary>
        /// Enables Windows broker flows on older platforms, such as .NET framework, where these are not available in the box with Microsoft.Identity.Client
        /// </summary>
        private static void AddSupportForWebView2(PublicClientApplicationBuilder builder)
        {
            builder.Config.WebUiFactoryCreator = () => new WebView2WebUiFactory();
        }
    }
}
