// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Describes the types of audiences for telemetry. <see cref="ITelemetryConfig.AudienceType"/>
    /// </summary>
    /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
    public enum TelemetryAudienceType
    {
        /// <summary>
        /// Indicates a PreProduction environment. PreProd environments are not sampled.
        /// </summary>
        PreProduction,

        /// <summary>
        /// Indicates a Production environment.  These environments are sampled based on the platforms' device info to reduce data load.
        /// </summary>
        Production
    }

    /// <summary>
    /// </summary>
    /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
    public interface ITelemetryConfig
    {
        /// <summary>
        /// Communicates which audience the telemetry is for (e.g. Production or Pre-Production) so that MSAL.NET can change sampling
        /// and filtering behavior.
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        TelemetryAudienceType AudienceType { get; }

        /// <summary>
        /// ID for the telemetry session.
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        string SessionId { get; }

        /// <summary>
        /// Implementers of the interface will receive this callback when telemetry data is available. The implementation should transfer
        /// the data in ITelemetryEventPayload to a specific telemetry uploader instance.
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        Action<ITelemetryEventPayload> DispatchAction { get; }
    }
}
