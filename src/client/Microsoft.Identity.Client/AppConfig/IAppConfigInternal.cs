// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    internal interface IAppConfigInternal : IAppConfig
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

        /// <summary>
        /// </summary>
        ClientCredentialWrapper ClientCredential { get; }

        /// <summary>
        /// Callback used for sending telemetry about MSAL.NET out of your app. It was set by a call
        /// to <see cref="AbstractApplicationBuilder{T}.WithTelemetry(TelemetryCallback)"/>
        /// </summary>
        TelemetryCallback TelemetryCallback { get; }

        /// <summary>
        /// Function pointer that creates a broker. Used by tests and by MSAL.Desktop to inject a broker.
        /// </summary>
        Func<CoreUIParent, ICoreLogger, IBroker> BrokerCreatorFunc { get; set; }

    }
}
