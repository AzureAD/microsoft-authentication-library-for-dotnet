// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// </summary>
    public enum MatsAudienceType
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
    public interface IMatsConfig
    {
        /// <summary>
        /// 
        /// </summary>
        MatsAudienceType AudienceType { get; }

        /// <summary>
        /// 
        /// </summary>
        string AppName { get; }

        /// <summary>
        /// 
        /// </summary>
        string AppVer { get; }

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

        /// <summary>
        /// todo(mats): i don't think we'll need this for MSAL.
        /// </summary>
        IEnumerable<string> AllowedResources { get; }
    }
}
