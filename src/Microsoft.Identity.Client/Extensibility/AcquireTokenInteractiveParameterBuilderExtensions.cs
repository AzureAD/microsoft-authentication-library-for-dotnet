// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Mats.Internal.Events;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// </summary>
    public static class AcquireTokenInteractiveParameterBuilderExtensions
    {
        /// <summary>
        ///     Extension method enabling MSAL.NET extenders for public client applications to set a custom web ui
        ///     that will let the user sign-in with Azure AD, present consent if needed, and get back the authorization
        ///     code
        /// </summary>
        /// <param name="builder">Builder for an AcquireTokenInteractive</param>
        /// <param name="customWebUi">Customer implementation for the Web UI</param>
        /// <returns>the builder to be able to chain .With methods</returns>
        public static AcquireTokenInteractiveParameterBuilder WithCustomWebUi(
            this AcquireTokenInteractiveParameterBuilder builder,
            ICustomWebUi customWebUi)
        {
            builder.CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithCustomWebUi);
            builder.SetCustomWebUi(customWebUi);
            return builder;
        }
    }
}
