// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    internal class MatsConfig : IMatsConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public MatsAudienceType AudienceType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Action<IMatsTelemetryBatch> DispatchAction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> AllowedScopes { get; set; }
    }
}
