// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal interface IApplicationConfiguration : IAppConfig
    {
        /// <summary>
        /// ExtendedLifeTimeEnabled is a Boolean that applications can set to true in case when the STS has an outage,
        /// to be more resilient.
        /// </summary>
        bool IsExtendedTokenLifetimeEnabled { get; }

        /// <summary>
        /// </summary>
        AuthorityInfo AuthorityInfo { get; }

        InstanceDiscoveryResponse CustomInstanceDiscoveryMetadata { get; }


#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        /// <summary>
        /// </summary>
        ClientCredentialWrapper ClientCredential { get; }

        /// <summary>
        /// Callback used for sending telemetry about MSAL.NET out of your app. It was set by a call
        /// to <see cref="AbstractApplicationBuilder{T}.WithTelemetry(TelemetryCallback)"/>
        /// </summary>
        TelemetryCallback TelemetryCallback { get; }
#endif
    }
}
