// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;

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

            builder.Config.IsBrokerEnabled = enableBroker;
            AddSupportForWam(builder);
            return builder;
        }

        internal static void AddSupportForWam(PublicClientApplicationBuilder builder)
        {
            builder.Config.BrokerCreatorFunc =
                (uiParent, appConfig, logger) => 
                    new Platforms.Features.WamBroker.WamBroker(uiParent, appConfig, logger);
        }
    }
}
