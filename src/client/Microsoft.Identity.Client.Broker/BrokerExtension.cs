// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
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
        public static PublicClientApplicationBuilder WithWindowsBroker(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
            builder.Config.IsBrokerEnabled = enableBroker;
            AddRuntimeSupportForWam(builder);
            return builder;
        }

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
        [Obsolete("Use WithWindowsBroker instead.", false)]
        public static PublicClientApplicationBuilder WithBrokerPreview(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
            builder.Config.IsBrokerEnabled = enableBroker;
            AddRuntimeSupportForWam(builder);
            return builder;
        }

        private static void AddRuntimeSupportForWam(PublicClientApplicationBuilder builder)
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
                   (uiParent, appConfig, logger) =>
                   {
                       logger.Info("[RuntimeBroker] Not a Windows 10 or Server equivalent machine. WAM is not available.");
                       return new NullBroker(logger);
                   };
            }
        }
    }
}
