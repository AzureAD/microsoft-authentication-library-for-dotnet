// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Desktop
{
    /// <summary>
    /// WAM related extensions.
    /// </summary>
    public static class WamExtension
    {
        /// <summary>
        /// Enables Windows broker flows on older platforms, such as .NET framework, where these are not available in the box with Microsoft.Identity.Client
        /// For details about Windows broker, see https://aka.ms/msal-net-wam
        /// </summary>
        public static PublicClientApplicationBuilder WithWindowsBroker(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
            builder.Config.IsBrokerEnabled = enableBroker;
            AddRuntimeSupportForWam(builder);
            return builder;
        }

        // this is for msal_runtime WAM
        private static void AddRuntimeSupportForWam(PublicClientApplicationBuilder builder)
        {

            if (DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                builder.Config.BrokerCreatorFunc =
                     (uiParent, appConfig, logger) =>
                     {
                         logger.Info("[WamBroker] WAM supported OS.");
                         return new RuntimeBroker(uiParent, appConfig, logger);
                     };
            }
            else
            {
                builder.Config.BrokerCreatorFunc =
                   (uiParent, appConfig, logger) =>
                   {
                       logger.Info("[WamBroker] Not a Windows 10 or Server equivalent machine. WAM is not available.");
                       return new NullBroker(logger);
                   };
            }
        }

        // this is for legacy WAM
        internal static void AddSupportForWam(PublicClientApplicationBuilder builder)
        {
            if (DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                builder.Config.BrokerCreatorFunc =
                     (uiParent, appConfig, logger) =>
                     {
                         logger.Info("WAM supported OS.");
                         return new Platforms.Features.WamBroker.WamBroker(uiParent, appConfig, logger);
                     };
            }
            else
            {
                builder.Config.BrokerCreatorFunc =
                   (uiParent, appConfig, logger) =>
                   {
                       logger.Info("Not a Win10 machine. WAM is not available");
                       return new NullBroker(logger);
                   };
            }
        }
    }
}
