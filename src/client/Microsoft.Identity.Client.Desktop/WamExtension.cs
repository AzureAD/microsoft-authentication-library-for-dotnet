// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ApiConfig;

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
        [Obsolete("To use the broker add ")]
        public static PublicClientApplicationBuilder WithWindowsBroker(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
            builder.Config.IdentityLogger?.Log(new IdentityModel.Abstractions.LogEntry() { EventLogLevel = IdentityModel.Abstractions.EventLogLevel.Informational, Message = "Desktop WAM Broker extension calling RuntimeBroker extension" });
            
            BrokerOptions options = new BrokerOptions(enableBroker ? BrokerOptions.OperatingSystems.Windows : BrokerOptions.OperatingSystems.None);
            
            return DesktopExtensions.WithWindowsDesktopFeatures(builder, options);
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
            DesktopExtensions.AddRuntimeSupportForWam(builder);

            builder.Config.BrokerOptions = brokerOptions;
            builder.Config.IsBrokerEnabled = brokerOptions.IsBrokerEnabledOnCurrentOs();

            return builder;
        }
    }
}
