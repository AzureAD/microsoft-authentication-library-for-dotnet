// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Desktop
{
    /// <summary>
    /// </summary>
    public static class WamExtension
    {
        /// <summary>
        /// Enables Windows broker flows on older platforms, such as .NET framework, where these are not available in the box with Microsoft.Identity.Client
        /// For details about Windows broker, see https://aka.ms/msal-net-wam
        /// </summary>
        public static PublicClientApplicationBuilder WithWindowsBroker(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
            if (!builder.Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(nameof(WithWindowsBroker)));
            }

            builder.Config.IsBrokerEnabled = true;
            builder.Config.BrokerCreatorFunc =
                (uiParent, logger) => new Platforms.Features.WamBroker.WamBroker(uiParent, logger);
            return builder;
        }
    }
}
