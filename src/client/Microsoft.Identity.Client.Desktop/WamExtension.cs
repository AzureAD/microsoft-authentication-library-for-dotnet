// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Desktop
{
    /// <summary>
    /// WAM related extensions.
    /// </summary>
    public static class WamExtension
    {
        /// <summary>
        /// Brokers enable Single-Sign-On, device identification, and enhanced security.
        /// Use this API to enable brokers on desktop platforms.
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
