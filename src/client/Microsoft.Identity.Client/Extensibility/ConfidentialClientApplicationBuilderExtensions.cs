// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility methods for <see cref="ConfidentialClientApplicationBuilder"/>
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public static class ConfidentialClientApplicationBuilderExtensions
    {
        /// <summary>
        /// Allows setting a callback which returns an access token, based on the passed-in parameters.
        /// MSAL will pass in its authentication parameters to the callback and it is expected that the callback
        /// will construct a <see cref="AppTokenProviderResult"/> and return it to MSAL.
        /// MSAL will cache the token response the same way it does for other authentication results.
        /// </summary>
        /// <remarks>This is part of an extensibility mechanism designed to be used only by Azure SDK in order to
        /// enhance managed identity support. Only client_credential flow is supported.</remarks>
        public static ConfidentialClientApplicationBuilder WithAppTokenProvider(
            this ConfidentialClientApplicationBuilder builder,
            Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> appTokenProvider)
        {
            builder.Config.AppTokenProvider = appTokenProvider ?? throw new ArgumentNullException(nameof(appTokenProvider));
            return builder;
        }
    }
}
