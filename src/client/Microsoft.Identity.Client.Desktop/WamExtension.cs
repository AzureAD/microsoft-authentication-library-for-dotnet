// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Broker;

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
            builder.Config.IdentityLogger?.Log(new IdentityModel.Abstractions.LogEntry() { EventLogLevel = IdentityModel.Abstractions.EventLogLevel.Informational, Message = "Desktop WAM Broker extension calling RuntimeBroker extension" });
            return BrokerExtension.WithWindowsBroker(builder, enableBroker);
        }
    }
}
