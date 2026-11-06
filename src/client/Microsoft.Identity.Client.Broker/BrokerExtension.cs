// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.RuntimeBroker;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Broker
{
    /// <summary>
    /// MSAL Broker Extension for WAM support
    /// </summary>
    public static class BrokerExtension
    {

        /// <summary>
        /// Brokers enable Single-Sign-On, device identification,and application identification verification, 
        /// while increasing the security of applications. Use this API to enable brokers on desktop platforms.
        /// 
        /// See https://aka.ms/msal-net-wam for more information on platform specific settings required to enable the broker such as redirect URIs.
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="brokerOptions">This provides cross platform options for broker.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static PublicClientApplicationBuilder WithBroker(this PublicClientApplicationBuilder builder, BrokerOptions brokerOptions)
        {
            AddRuntimeSupport(builder);
            builder.Config.BrokerOptions = brokerOptions;
            builder.Config.IsBrokerEnabled = brokerOptions.IsBrokerEnabledOnCurrentOs();
            return builder;
        }

        /// <summary>
        /// Use this API to enable SsoPolicy enforcement. 
        /// Should only be utilized by Microsoft 1st party applications.
        /// This is applicable only when broker is not enabled and embedded webview is the preferred choice.
        /// By default, the broker supports SsoPolicy, and system webview SsoPolicy is also supported at the OS level.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static PublicClientApplicationBuilder WithSsoPolicy(this PublicClientApplicationBuilder builder)
        {
            AddRuntimeSupport(builder);
            builder.Config.IsWebviewSsoPolicyEnabled = true;
            return builder;
        }

        private static void AddRuntimeSupport(PublicClientApplicationBuilder builder)
        {
            if (DesktopOsHelper.IsWin10OrServerEquivalent() || DesktopOsHelper.IsLinux() || DesktopOsHelper.IsMac())
            {
                 builder.Config.BrokerCreatorFunc =
                     (uiParent, appConfig, logger) =>
                     {
                         logger.Info("[Runtime] Broker supported OS.");
                         return new RuntimeBroker(uiParent, appConfig, logger);
                     };
            }
            else
            {
                builder.Config.BrokerCreatorFunc =
                   (uiParent, appConfig, logger) =>
                   {
                       logger.Warning($"[RuntimeBroker] Not available on the current platform.");
                       return new NullBroker(logger);
                   };
            }
        }
    }
}
