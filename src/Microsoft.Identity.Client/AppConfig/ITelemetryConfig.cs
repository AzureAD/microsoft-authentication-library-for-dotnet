// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public enum TelemetryAudienceType
    {
        /// <summary>
        /// 
        /// </summary>
        PreProduction,

        /// <summary>
        /// 
        /// </summary>
        Production
    }

    /// <summary>
    /// </summary>
    public interface ITelemetryConfig
    {
        /// <summary>
        /// 
        /// </summary>
        TelemetryAudienceType AudienceType { get; }

        /// <summary>
        /// 
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// 
        /// </summary>
        Action<ITelemetryEventPayload> DispatchAction { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<string> AllowedScopes { get; }
    }
}
