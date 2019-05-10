// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    internal enum MatsAudienceType
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
    internal interface IMatsConfig
    {
        /// <summary>
        /// 
        /// </summary>
        MatsAudienceType AudienceType { get; }

        /// <summary>
        /// 
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// 
        /// </summary>
        Action<IMatsTelemetryBatch> DispatchAction { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<string> AllowedScopes { get; }
    }
}
