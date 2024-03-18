// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.RuntimeBroker;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

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
        [Obsolete("Use WithWindowsDesktopFeatures instead. For broker support only, use  WithBroker(BrokerOptions) from Microsoft.Identity.Client.Broker package.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PublicClientApplicationBuilder WithDesktopFeatures(this PublicClientApplicationBuilder builder)
        {
            builder.WithWindowsDesktopFeatures(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
            return builder;
        }

        /// <summary>
        /// Adds enhanced support for desktop applications, e.g. CLI, WinForms, WPF apps.
        /// - Windows Authentication Manager (WAM) broker, the recommended authentication mechanism on Windows 10+ - https://aka.ms/msal-net-wam
        /// - Embedded web view. AAD applications use the older WebBrowser control. Other applications (B2C, ADFS etc.) use WebView2, an embedded browser based on Microsoft Edge - https://aka.ms/msal-net-webview2
        /// </summary>
        /// <remarks>This is not required for MAUI / WinUI applications</remarks>
        public static PublicClientApplicationBuilder WithWindowsDesktopFeatures(this PublicClientApplicationBuilder builder, BrokerOptions brokerOptions)
        {
            builder.Config.BrokerOptions = brokerOptions;
            builder.Config.IsBrokerEnabled = brokerOptions.IsBrokerEnabledOnCurrentOs();

            AddRuntimeSupportForWam(builder);
            WithWindowsEmbeddedBrowserSupport(builder);

            return builder;
        }

        /// <summary>
        /// Adds better embedded browser support to MSAL.
        /// AAD applications will use the older WebBrowser control.
        /// Other applications (B2C, ADFS etc.) will use an embedded browser based on Microsoft Edge - https://aka.ms/msal-net-webview2
        /// </summary>
        /// <remarks>This is not required for MAUI / WinUI applications. This is ignored on Mac and Linux.</remarks>    
        /// 
        public static PublicClientApplicationBuilder WithWindowsEmbeddedBrowserSupport(this PublicClientApplicationBuilder builder)
        {
            if (DesktopOsHelper.IsWindows())
            {
                builder.Config.WebUiFactoryCreator = () => new WebView2WebUiFactory();
            }

            return builder;
        }

        internal static void AddRuntimeSupportForWam(PublicClientApplicationBuilder builder)
        {
            if (DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                builder.Config.BrokerCreatorFunc =
                     (uiParent, appConfig, logger) =>
                     {
                         logger.Info("[RuntimeBroker] WAM supported OS.");
                         return new RuntimeBroker(uiParent, appConfig, logger);
                     };
            }
            else
            {
                builder.Config.BrokerCreatorFunc =
                   (_, _, logger) =>
                   {
                       logger.Info("[RuntimeBroker] Not a Windows 10 or Server equivalent machine. WAM is not available.");
                       return new NullBroker(logger);
                   };
            }
        }
    }
}
