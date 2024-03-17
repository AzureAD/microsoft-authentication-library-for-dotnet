// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.MsalCppRuntime;
using Microsoft.Identity.Client.Platforms.Features.MsalCppRuntime;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Broker
{
    /// <summary>
    /// MSAL Broker Extension for WAM support
    /// </summary>
    public static class BrokerExtension
    {
        /// <summary>
        /// Enables MSAL to use Broker flows, which are more secure than browsers. 
        /// For details about Windows broker, see https://aka.ms/msal-net-wam
        /// </summary>
        /// <remarks>
        /// No broker integration exists on Mac and Linux yet.
        /// Windows broker does not work on Win 8, Win Server 2016 and lower.
        /// This implementation is not supported on UWP, use <c>WithBroker()</c> from Microsoft.Identity.Client package instead.
        /// If a broker does not exist or cannot be used, MSAL will fallback to a browser.
        /// Make sure browser auth is enabled (e.g. if using system browser, register the "http://localhost" redirect URI, etc.)
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This broker implementation is generally available. Use WithBroker(BrokerOptions) in Microsoft.Identity.Client.Broker package instead. See https://aka.ms/msal-net-wam for details.", false)]
        public static PublicClientApplicationBuilder WithBrokerPreview(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
            WithBroker(builder, new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
            return builder;
        }

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
            AddRuntimeSupport(builder, true);
            builder.Config.BrokerOptions = brokerOptions;
            builder.Config.IsBrokerEnabled = brokerOptions.IsBrokerEnabledOnCurrentOs();
            return builder;
        }

        /// <summary>
        /// Use this API to enable SsoPolicy enforcement when not using broker.
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static PublicClientApplicationBuilder WithSsoPolicy(this PublicClientApplicationBuilder builder)
        {
            AddRuntimeSupport(builder, false);
            builder.Config.IsSsoPolicyEnabled = true;
            return builder;
        }

        private static void AddRuntimeSupport(PublicClientApplicationBuilder builder, bool isWamBrokerScenario)
        {
            if (DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                if(isWamBrokerScenario)
                {
                    builder.Config.RuntimeBrokerCreatorFunc =
                         (uiParent, appConfig, logger) =>
                         {
                             logger.Info("[Runtime] WAM supported OS.");
                             return new MsalCppRuntime(uiParent, appConfig, logger);
                         };
                }
                else
                {
                    builder.Config.RuntimeSsoPolicyCreatorFunc =
                         (appConfig, logger) =>
                         {
                             logger.Info("[Runtime] SsoPolicy supported OS.");
                             return new MsalCppRuntime(appConfig, logger);
                         };
                }
            }
            else
            {
                builder.Config.RuntimeBrokerCreatorFunc =
                   (uiParent, appConfig, logger) =>
                   {
                       logger.Info("[Runtime] Not a Windows 10 or Server equivalent machine. WAM is not available.");
                       return new NullRuntime(logger);
                   };
                builder.Config.RuntimeSsoPolicyCreatorFunc =
                   (appConfig, logger) =>
                   {
                       logger.Info("[Runtime] Not a Windows 10 or Server equivalent machine. SsoPolicy is not available.");
                       return new NullRuntime(logger);
                   };
            }
        }
    }
}
